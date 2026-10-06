using System.Net;
using System.Text.RegularExpressions;

namespace BlazorApp.RMTWebsite.Tests.Integration;

/// <summary>
/// Every content page starts with the shared PageHeader (plan task 2.8): one &lt;h1&gt; inside
/// header.page-header, its own .container placed first in &lt;main&gt; (not nested in the page's
/// container), page content in a .container right after it, and no &lt;hr&gt;/&lt;br&gt; spacing.
/// Home (its hero is the header) and Error (rewritten in 3.5) are excluded.
/// </summary>
public class PageHeaderTests(SiteFactory factory) : IClassFixture<SiteFactory>
{
    /// <summary>Any unknown URL shows the Not Found page (with a 404 status for search engines).</summary>
    private const string NotFoundUrl = "/no-such-page";

    /// <summary>Each content page, its agreed h1, and its subtitle (null when it has none).</summary>
    private static readonly (string Url, string H1, string? Subtitle)[] Pages =
    [
        ("/services", "Services and rates", "Registered massage therapy in Guelph, Ontario"),
        ("/about", "Nicholas Turco, RMT", "Registered massage therapist in Guelph, Ontario"),
        ("/contact", "Contact us", "Questions or appointment requests? Send a message below."),
        ("/faq", "Frequently asked questions", null),
        (NotFoundUrl, "Page not found", null),
    ];

    /// <summary>Every page with its wording, for the heading test.</summary>
    public static TheoryData<string, string, string?> ContentPages()
    {
        var data = new TheoryData<string, string, string?>();
        foreach (var (url, h1, subtitle) in Pages)
        {
            data.Add(url, h1, subtitle);
        }
        return data;
    }

    /// <summary>Just the URLs, for the layout tests that don't need the wording.</summary>
    public static TheoryData<string> ContentPageUrls() => new(Pages.Select(page => page.Url));

    /// <summary>
    /// Matches a class attribute that contains <paramref name="className"/> as a whole class, so
    /// "container" matches class="container table-info" but not class="container-fluid".
    /// </summary>
    private static string HasClass(string className) =>
        $@"\bclass=""(?:[^""]*\s)?{Regex.Escape(className)}(?:\s[^""]*)?""";

    /// <summary>
    /// Fetches a page's HTML and checks its status: 404 for the Not Found page, 200 for the rest.
    /// (GetStringAsync can't be used because it throws on the 404.) Checking the status first means
    /// a page that crashes fails with "expected OK, got 500", not a confusing wording mismatch.
    /// </summary>
    private async Task<string> GetHtmlAsync(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, TestContext.Current.CancellationToken);
        var expectedStatus = url == NotFoundUrl ? HttpStatusCode.NotFound : HttpStatusCode.OK;
        Assert.Equal(expectedStatus, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>The page's header.page-header element, or a failed match if there isn't one.</summary>
    private static Match GetPageHeader(string html) =>
        Regex.Match(html, $@"<header\b[^>]*{HasClass("page-header")}[^>]*>.*?</header>", RegexOptions.Singleline);

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

        var subtitle = Regex.Match(header.Value, $@"<p\b[^>]*{HasClass("page-header-subtitle")}[^>]*>(.*?)</p>", RegexOptions.Singleline);
        if (expectedSubtitle is null)
        {
            Assert.False(subtitle.Success, $"{url} shouldn't have a subtitle.");
        }
        else
        {
            Assert.Equal(expectedSubtitle, Text(subtitle.Groups[1].Value));
        }
    }

    // PageHeader's own .container is the first thing in <main>. Anchoring to <main> is what makes this
    // a real check: PageHeader always renders div.container > header, so only the position tells us
    // whether a page wrapped it in a second container (which doubles the side padding)
    [Theory]
    [MemberData(nameof(ContentPageUrls))]
    public async Task Get_ContentPage_PageHeaderContainerIsFirstInMain(string url)
    {
        // Arrange & Act
        var html = await GetHtmlAsync(url);

        // Assert
        Assert.Matches($@"<main\b[^>]*>\s*<div\b[^>]*{HasClass("container")}[^>]*>\s*<header\b[^>]*{HasClass("page-header")}", html);
    }

    // The page's content starts in its own .container straight after PageHeader's, so both share a
    // left edge at every width
    [Theory]
    [MemberData(nameof(ContentPageUrls))]
    public async Task Get_ContentPage_ContentAfterHeaderIsInContainer(string url)
    {
        // Arrange
        var html = await GetHtmlAsync(url);

        // Act: everything after </header>, which should be PageHeader's closing </div> and then the content
        var header = GetPageHeader(html);
        var afterHeader = html[(header.Index + header.Length)..];

        // Assert
        Assert.True(header.Success, $"{url} has no header.page-header.");
        Assert.Matches($@"^\s*</div>\s*<div\b[^>]*{HasClass("container")}", afterHeader);
    }

    // The divider and space under the heading come from PageHeader's CSS, not an <hr>/<br> after it
    [Theory]
    [MemberData(nameof(ContentPageUrls))]
    public async Task Get_ContentPage_NoHrOrBrAfterHeader(string url)
    {
        // Arrange
        var html = await GetHtmlAsync(url);

        // Act: the first opening tag after </header> (closing tags like </div> are skipped)
        var header = GetPageHeader(html);
        var nextTag = Regex.Match(html[(header.Index + header.Length)..], @"<([a-zA-Z][a-zA-Z0-9]*)").Groups[1].Value;

        // Assert
        Assert.True(header.Success, $"{url} has no header.page-header.");
        Assert.NotEmpty(nextTag);
        Assert.DoesNotContain(nextTag.ToLowerInvariant(), new[] { "hr", "br" });
    }
}
