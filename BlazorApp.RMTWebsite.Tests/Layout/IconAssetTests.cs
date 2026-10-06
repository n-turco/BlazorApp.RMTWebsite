using System.Text.RegularExpressions;
using BlazorApp.RMTWebsite.Tests.Helpers;

namespace BlazorApp.RMTWebsite.Tests.Layout;

/// <summary>
/// Checks the favicon set (plan task 2.1, logo and favicon). Like fonts, a broken icon link never
/// causes an error: the browser just shows a blank tab icon. The test project copies App.razor and
/// the icon files into SiteFiles/ (see the .csproj).
/// </summary>
public class IconAssetTests
{
    /// <summary>The copy of the site's wwwroot icon files in the test output.</summary>
    private static readonly string WwwrootPath = Path.Combine(AppContext.BaseDirectory, "SiteFiles", "wwwroot");

    /// <summary>
    /// Returns (rel, href) for every &lt;link&gt; in App.razor's head whose rel is "icon" or
    /// "apple-touch-icon". An href of @Assets["x"] is read as "x", the file it fingerprints.
    /// </summary>
    private static List<(string Rel, string Href)> GetIconLinks()
    {
        var appRazor = RazorSource.ReadWithoutComments("App.razor");

        return Regex.Matches(appRazor, @"<link\b[^>]*>")
            .Select(tag => (
                Rel: Regex.Match(tag.Value, @"\brel=""([^""]+)""").Groups[1].Value,
                Href: Regex.Match(tag.Value, @"\bhref=""(?:@Assets\[""?)?([^""\]]+)").Groups[1].Value))
            .Where(link => link.Rel is "icon" or "apple-touch-icon")
            .ToList();
    }

    // Some clients (search crawlers, link previews) request /favicon.ico directly, whatever the HTML says
    [Fact]
    public void Wwwroot_FaviconIco_ExistsAtRoot()
    {
        // Arrange
        var path = Path.Combine(WwwrootPath, "favicon.ico");

        // Act
        var exists = File.Exists(path);

        // Assert
        Assert.True(exists, "wwwroot/favicon.ico should exist so /favicon.ico doesn't return 404.");
    }

    // A wrong file name would leave a blank tab icon with no error anywhere
    [Fact]
    public void AppRazor_IconLinks_PointToExistingFiles()
    {
        // Arrange
        var links = GetIconLinks();

        // Act
        var missing = links
            .Where(link => !File.Exists(Path.Combine(WwwrootPath, link.Href)))
            .Select(link => link.Href)
            .ToList();

        // Assert
        Assert.NotEmpty(links);
        Assert.Empty(missing);
    }

    // The Blazor template's purple "@" icon should be gone, replaced by the KF monogram set
    [Fact]
    public void AppRazor_IconLinks_IncludeSvgIcoAndAppleTouchIcon()
    {
        // Arrange
        var links = GetIconLinks();

        // Act
        var hrefs = links.Select(link => link.Href).ToList();

        // Assert
        Assert.Contains("favicon.svg", hrefs);
        Assert.Contains("favicon.ico", hrefs);
        Assert.Contains("apple-touch-icon.png", hrefs);
        Assert.DoesNotContain("favicon.png", hrefs);
    }
}
