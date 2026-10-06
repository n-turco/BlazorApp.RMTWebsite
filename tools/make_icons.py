"""Generate the KF monogram favicon set from the self-hosted Lora font (plan 2.1, logo/favicon).

Outputs (into wwwroot): favicon.svg, favicon.ico (16+32), apple-touch-icon.png (180).
Dev-only tool: not part of the build, and the generated files are committed. Re-run it only when
the mark, colour, or font changes. See Visual_dataflow_features/2.1-logo-favicon.md.

Usage (from the repo root, in a throwaway virtual environment):
    python -m venv .venv-icons
    .venv-icons/Scripts/python -m pip install fonttools brotli pillow
    .venv-icons/Scripts/python tools/make_icons.py BlazorApp.RMTWebsite/wwwroot
"""
import sys
from pathlib import Path

from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools.pens.basePen import BasePen
from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from PIL import Image, ImageDraw

WWWROOT = Path(sys.argv[1])
BRAND_PRIMARY = "#007099"   # --brand-primary in app.css; white on it is 5.56:1
LETTERS = "KF"
WEIGHT = 700                # bold reads better at 16px
BOX = 100                   # SVG viewBox is 0 0 100 100
RADIUS = 22                 # rounded-square corner radius
MAX_W, MAX_H = 70, 54       # letters fit inside this, centred
GAP = 0.04                  # space between K and F, as a fraction of cap height


class FlattenPen(BasePen):
    """Turns glyph outlines into straight-line polygons so Pillow can fill them."""

    def __init__(self, glyphset, steps=24):
        super().__init__(glyphset)
        self.polygons, self.current, self.steps = [], [], steps

    def _moveTo(self, p):
        self.current = [p]

    def _lineTo(self, p):
        self.current.append(p)

    def _curveToOne(self, p1, p2, p3):
        p0 = self._getCurrentPoint()
        for i in range(1, self.steps + 1):
            t = i / self.steps
            mt = 1 - t
            self.current.append(tuple(mt**3 * a + 3 * mt * mt * t * b + 3 * mt * t * t * c + t**3 * d
                                      for a, b, c, d in zip(p0, p1, p2, p3)))

    def _qCurveToOne(self, p1, p2):
        p0 = self._getCurrentPoint()
        for i in range(1, self.steps + 1):
            t = i / self.steps
            mt = 1 - t
            self.current.append(tuple(mt * mt * a + 2 * mt * t * b + t * t * c for a, b, c in zip(p0, p1, p2)))

    def _closePath(self):
        self.polygons.append(self.current)
        self.current = []

    _endPath = _closePath


# 1. A static bold instance of the variable Lora font
font = instantiateVariableFont(TTFont(WWWROOT / "fonts" / "lora-latin-wght-normal.woff2"), {"wght": WEIGHT})
glyphset = font.getGlyphSet()
cmap = font.getBestCmap()
names = [cmap[ord(ch)] for ch in LETTERS]

# 2. Lay the letters out side by side by their ink bounds (font units, y up)
bounds = []
for n in names:
    bp = BoundsPen(glyphset)
    glyphset[n].draw(bp)
    bounds.append(bp.bounds)
cap_h = max(b[3] for b in bounds) - min(b[1] for b in bounds)
offsets, x = [], 0.0
for b in bounds:
    offsets.append(x - b[0])          # shift so this letter's ink starts at x
    x += (b[2] - b[0]) + GAP * cap_h
total_w = x - GAP * cap_h
y_min, y_max = min(b[1] for b in bounds), max(b[3] for b in bounds)

# 3. Scale into the box and centre; flip y because SVG/image y points down
s = min(MAX_W / total_w, MAX_H / (y_max - y_min))
tx = (BOX - total_w * s) / 2
ty = (BOX + (y_max - y_min) * s) / 2 + y_min * s


def transform_for(i):
    return (s, 0, 0, -s, tx + offsets[i] * s, ty)


# 4. favicon.svg: rounded square + letter outlines (no font needed to display it)
paths = []
for i, n in enumerate(names):
    sp = SVGPathPen(glyphset, ntos=lambda v: f"{v:.1f}".rstrip("0").rstrip("."))
    glyphset[n].draw(TransformPen(sp, transform_for(i)))
    paths.append(sp.getCommands())
svg = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {BOX} {BOX}">'
       f'<rect width="{BOX}" height="{BOX}" rx="{RADIUS}" fill="{BRAND_PRIMARY}"/>'
       f'<path fill="#fff" d="{" ".join(paths)}"/></svg>\n')
(WWWROOT / "favicon.svg").write_text(svg, encoding="utf-8")


# 5. Raster versions drawn from the same outlines, 8x supersampled then scaled down for smooth edges
def render(size, rounded=True):
    big = size * 8
    k = big / BOX
    img = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if rounded:
        d.rounded_rectangle([0, 0, big - 1, big - 1], radius=RADIUS * k, fill=BRAND_PRIMARY)
    else:
        d.rectangle([0, 0, big, big], fill=BRAND_PRIMARY)   # iOS adds its own rounding; no transparency allowed
    for i, n in enumerate(names):
        fp = FlattenPen(glyphset)
        glyphset[n].draw(TransformPen(fp, transform_for(i)))
        for poly in fp.polygons:
            d.polygon([(px * k, py * k) for px, py in poly], fill="white")
    return img.resize((size, size), Image.LANCZOS)


render(180, rounded=False).convert("RGB").save(WWWROOT / "apple-touch-icon.png", optimize=True)
icon32, icon16 = render(32), render(16)
icon32.save(WWWROOT / "favicon.ico", sizes=[(16, 16), (32, 32)], append_images=[icon16])

print("Wrote favicon.svg, favicon.ico and apple-touch-icon.png to", WWWROOT)
