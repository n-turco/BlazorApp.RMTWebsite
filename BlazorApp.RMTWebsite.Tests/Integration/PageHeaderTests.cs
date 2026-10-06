using System.Net;
using System.Text.RegularExpressions;

namespace BlazorApp.RMTWebsite.Tests.Integration;

/// <summary>
/// Every content page starts with the shared PageHeader (plan task 2.8): one &lt;h1&gt; inside
/// header.page-header, its own .container so it lines up with the content, and no &lt;hr&gt;/&lt;br&gt;
/// spacing straight after it. Home (its hero is the header) and Error (rewritten in 3.5) are excluded.
/// </summary>
public class PageHeaderTests(SiteFactory factory) : IClassFixture<SiteFactory>
{
    /// <summary>
    /// Each content page, its agreed h1, and its subtitle (null when it has none). "/no-such-page"
    /// stands for the Not Found page, which is shown for any unknown URL.
    /// </summary>
    public static TheoryData<string, string, string?> ContentPages() => new()
    {
        { "/services", "Services and rates", "Registered massage therapy in Guelph, Ontario" },
        { "/about", "Nicholas Turco, RMT", "Registered massage therapist in Guelph, Ontario" },
        { "/contact", "Contact us", "Questions or appointment requests? Send a message below." },
        { "/faq", "Frequently asked questions", null },
        { "/no-such-page", "Page not found", null },
    };

    /// <summary>
    /// Fetches a page's HTML whatever its status code (the Not Found page comes back as a 404 with
    /// its content, so GetStringAsync, which throws on non-success, can't be used).
    /// </summary>
    private async Task<string> GetHtmlAsync(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, TestContext.Current.CancellationToken);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>The page's header.page-header element, or a failed match if there isn't one.</summary>
    private static Match GetPageHeader(string html) =>
        Regex.Match(html, @"<header\b[^>]*\bclass=""[^""]*\bpage-header\b[^""]*""[^>]*>.*?</header>", RegexOptions.Singleline);

    /// <summary>Strips tags, decodes entities, and collapses whitespace, e.g. "&lt;h1&gt; FAQ &lt;/h1&gt;" becomes "FAQ".</summary>
    private static string Text(string markup) =>
        Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(markup, "<[^>]+>", "")), @"\s+", " ").Trim();

    // The page's only <h1> comes from PageHeader, with the agreed wording, and the subtitle when there is one
    [Theory]
    [MemberData(nameof(ContentPages))]
    public async Task Get_ContentPage_RendersSinglePageHeaderH1(string url, string expectedH1, string? expectedSubtitle)
    {
        // Arrange & Act
        var html = await GetHtmlAsync(url);
        var header = GetPageHeader(html);
        var h1Tags = Regex.Matches(html, @"<h1\b[^>]*>(.*?)</h1>", RegexOptions.Singleline);

        // Assert
        Assert.True(header.Success, $"{url} has no header.page-header.");
        var h1 = Assert.Single(h1Tags);
        Assert.Equal(expectedH1, Text(h1.Groups[1].Value));
        Assert.Contains(h1.Value, header.Value);

        var subtitle = Regex.Match(header.Value, @"<p\b[^>]*\bpage-header-subtitle\b[^>]*>(.*?)</p>", RegexOptions.Singleline);
        if (expectedSubtitle is null)
        {
            Assert.False(subtitle.Success, $"{url} shouldn't have a subtitle.");
        }
        else
        {
            Assert.Equal(expectedSubtitle, Text(subtitle.Groups[1].Value));
        }
    }

    // PageHeader brings its own .container, so its left edge always matches the page content below it
    [Theory]
    [MemberData(nameof(ContentPages))]
    public async Task Get_ContentPage_PageHeaderIsInsideContainer(string url, string expectedH1, string? expectedSubtitle)
    {
        // Arrange
        _ = (expectedH1, expectedSubtitle); // shared test data; only the URL matters here

        // Act
        var html = await GetHtmlAsync(url);

        // Assert
        Assert.Matches(@"<div\b[^>]*\bclass=""[^""]*\bcontainer\b[^""]*""[^>]*>\s*<header\b[^>]*\bpage-header\b", html);
    }

    // The divider and space under the heading come from PageHeader's CSS, not an <hr>/<br> after it
    [Theory]
    [MemberData(nameof(ContentPages))]
    public async Task Get_ContentPage_NoHrOrBrAfterHeader(string url, string expectedH1, string? expectedSubtitle)
    {
        // Arrange
        _ = (expectedH1, expectedSubtitle); // shared test data; only the URL matters here
        var html = await GetHtmlAsync(url);
        var header = GetPageHeader(html);
        Assert.True(header.Success, $"{url} has no header.page-header.");

        // Act: the first opening tag after </header> (closing tags like </div> are skipped)
        var nextTag = Regex.Match(html[(header.Index + header.Length)..], @"<([a-zA-Z][a-zA-Z0-9]*)").Groups[1].Value;

        // Assert
        Assert.DoesNotContain(nextTag.ToLowerInvariant(), new[] { "hr", "br" });
    }
}
