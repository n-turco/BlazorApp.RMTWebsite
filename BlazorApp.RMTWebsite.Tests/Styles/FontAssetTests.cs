using System.Text.RegularExpressions;

namespace BlazorApp.RMTWebsite.Tests.Styles;

/// <summary>
/// Checks that the self-hosted brand fonts (plan 2.1, ADR 0003) are wired up correctly. A broken
/// font URL never causes an error: the browser quietly shows the fallback font. These tests are the
/// only thing that catches a typo'd file name. The test project copies app.css, the font files and
/// App.razor into its output folder under SiteFiles/ (see the .csproj).
/// </summary>
public class FontAssetTests
{
    /// <summary>The copy of the site's wwwroot folder in the test output.</summary>
    private static readonly string WwwrootPath = Path.Combine(AppContext.BaseDirectory, "SiteFiles", "wwwroot");

    /// <summary>The copy of the site's HTML shell in the test output.</summary>
    private static readonly string AppRazorPath = Path.Combine(AppContext.BaseDirectory, "SiteFiles", "Components", "App.razor");

    /// <summary>
    /// Returns the URL inside every url(...) of every @font-face rule in app.css, e.g.
    /// "fonts/lora-latin-wght-normal.woff2". Quotes around the URL are optional in CSS.
    /// </summary>
    private static List<string> GetFontFaceUrls()
    {
        var css = File.ReadAllText(Path.Combine(WwwrootPath, "app.css"));

        return Regex.Matches(css, @"@font-face\s*\{[^}]*\}")
            .SelectMany(rule => Regex.Matches(rule.Value, @"url\(\s*['""]?([^'"")]+)['""]?\s*\)"))
            .Select(url => url.Groups[1].Value)
            .ToList();
    }

    // Guards the next test: if the regex stopped matching, "every URL exists" would pass on zero URLs
    [Fact]
    public void AppCss_FontFaceRules_DeclareHeadingAndBodyFonts()
    {
        // Arrange & Act
        var urls = GetFontFaceUrls();

        // Assert
        Assert.Equal(2, urls.Count);
    }

    // A wrong file name would silently show the fallback font instead of the brand font
    [Fact]
    public void AppCss_FontFaceUrls_PointToExistingFiles()
    {
        // Arrange
        var urls = GetFontFaceUrls();

        // Act: URLs in app.css are relative to app.css, which sits in the wwwroot root
        var missing = urls
            .Where(url => !File.Exists(Path.Combine(WwwrootPath, url)))
            .ToList();

        // Assert
        Assert.Empty(missing);
    }

    // If the preload URL differs from the @font-face URL, the browser downloads the font twice
    [Fact]
    public void AppRazor_FontPreload_MatchesAFontFaceUrl()
    {
        // Arrange
        var appRazor = File.ReadAllText(AppRazorPath);
        var fontFaceUrls = GetFontFaceUrls();

        // Act
        var preload = Regex.Match(appRazor, @"<link\s+rel=""preload""\s+href=""([^""]+)""\s+as=""font""");

        // Assert
        Assert.True(preload.Success, "App.razor should preload the body font.");
        Assert.Contains(preload.Groups[1].Value, fontFaceUrls);
    }
}
