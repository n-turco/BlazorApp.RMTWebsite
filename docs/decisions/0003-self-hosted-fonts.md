# 0003. Self-host brand fonts instead of linking Google Fonts

**Date:** 2026-10-05
**Status:** Accepted

## Context

Plan task 2.1 adds two brand fonts: Lora for headings and Inter for body text. Both are free under
the SIL Open Font License (OFL) and available from Google Fonts. There are two ways to load them:

- **Link Google Fonts** with a `<link>` to `fonts.googleapis.com`. It's one line of HTML, but every
  page view makes the visitor's browser contact Google, which sends Google the visitor's IP address.
  This is a health-care business, so we want to share as little visitor data as possible. In 2022 a
  German court found that embedding Google Fonts this way breached the GDPR. The browser also has to
  open connections to two more domains before it can draw the text.
- **Self-host:** put the `.woff2` files in `wwwroot/fonts/` and declare them with `@font-face`.

## Decision

Self-host the fonts.

- The font files come from Fontsource (`@fontsource-variable/lora` and `@fontsource-variable/inter`,
  v5.3.0), which repackages the Google Fonts files. We take only the **Latin** variable-weight file
  for each font, about 86 KB for both together. The site is English-only.
- Each font's OFL license ships next to it (`wwwroot/fonts/*-OFL.txt`), as the license requires.
- `@font-face` rules and the `--font-heading` / `--font-body` tokens live in `wwwroot/app.css`.
- Only the body font is preloaded in `App.razor`, because it's needed on every line of every page.
- No new library or build step. To update a font, download the new file and replace the old one.

## Consequences

- No third-party requests and no visitor data shared to show text. The site works the same if
  Google is unreachable.
- The fonts are served and cached the same way as `app.css` and the images.
- We own updates: font fixes don't arrive automatically. That's acceptable because fonts rarely change.
- If we add another language with non-Latin characters, we'll need to add that subset's file and a
  `unicode-range`. Until then, those characters fall back to the system font.
- A wrong file name in `@font-face` fails silently, because the browser just shows the fallback font.
  `FontAssetTests` checks that every font URL in `app.css` points to a real file.
