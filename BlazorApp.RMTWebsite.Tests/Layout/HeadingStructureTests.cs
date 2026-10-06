using System.Text.RegularExpressions;
using BlazorApp.RMTWebsite.Tests.Helpers;

namespace BlazorApp.RMTWebsite.Tests.Layout;

/// <summary>
/// Checks the heading structure (plan task 2.4): the shared layout has no &lt;h1&gt;, and every
/// routable page has exactly one &lt;h1&gt; naming that page. Screen-reader users jump between
/// headings, and search engines use the &lt;h1&gt;, so a site-wide &lt;h1&gt; makes every page look the same.
/// These tests read the .razor source files, which the test project copies into SiteFiles/
/// (see the .csproj). Rendered-HTML checks replace them once 6.2 PR 2 adds WebApplicationFactory.
/// </summary>
public class HeadingStructureTests
{
    /// <summary>Counts opening &lt;h1&gt; tags, with or without attributes. Doesn't match &lt;h10&gt; or &lt;hr&gt;.</summary>
    private static int CountH1Tags(string markup) => Regex.Matches(markup, @"<h1[\s>]", RegexOptions.IgnoreCase).Count;

    /// <summary>
    /// Every page with an @page directive, as a file name such as "FAQ.razor". Each one becomes a
    /// separate test case, so a failure names the page.
    /// </summary>
    public static TheoryData<string> RoutablePages()
    {
        var pages = new TheoryData<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(RazorSource.ComponentsPath, "Pages"), "*.razor"))
        {
            if (File.ReadAllText(file).Contains("@page"))
            {
                pages.Add(Path.GetFileName(file));
            }
        }
        return pages;
    }

    // The layout wraps every page, so an <h1> here would be repeated on every page
    [Fact]
    public void MainLayout_Header_ContainsNoH1()
    {
        // Arrange
        var layout = RazorSource.ReadWithoutComments(Path.Combine("Layout", "MainLayout.razor"));

        // Act
        var h1Count = CountH1Tags(layout);

        // Assert
        Assert.Equal(0, h1Count);
    }

    // Each page needs its own single <h1> now that the layout doesn't supply one
    [Theory]
    [MemberData(nameof(RoutablePages))]
    public void Page_EachRoutablePage_HasExactlyOneH1(string pageFile)
    {
        // Arrange
        var page = RazorSource.ReadWithoutComments(Path.Combine("Pages", pageFile));

        // Act
        var h1Count = CountH1Tags(page);

        // Assert
        Assert.Equal(1, h1Count);
    }
}
