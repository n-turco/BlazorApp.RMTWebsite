using System.Net;
using System.Text.RegularExpressions;

namespace BlazorApp.RMTWebsite.Tests.Integration;

/// <summary>
/// Requests every page through the real site (in memory) and checks the HTML that comes back.
/// Unlike the source-file tests, these see the final rendered page: layout, components and all.
/// </summary>
public class PageRoutingTests(SiteFactory factory) : IClassFixture<SiteFactory>
{
    /// <summary>Returns the text inside the page's &lt;title&gt; element.</summary>
    private static string GetTitle(string html) =>
        WebUtility.HtmlDecode(Regex.Match(html, @"<title>(.*?)</title>", RegexOptions.Singleline).Groups[1].Value.Trim());

    // Every page in the nav loads and sets its own <title> (shown in the browser tab and search results)
    [Theory]
    [InlineData("/", "Home")]
    [InlineData("/services", "Services and rates")]
    [InlineData("/about", "About Me")]
    [InlineData("/contact", "Contact Us")]
    [InlineData("/faq", "FAQ")]
    public async Task Get_KnownRoute_ReturnsOkWithPageTitle(string url, string expectedTitle)
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedTitle, GetTitle(html));
    }

    // UseStatusCodePagesWithReExecute shows the Not Found page but keeps the 404 status for crawlers
    [Fact]
    public async Task Get_UnknownRoute_ReturnsNotFoundPage()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/no-such-page", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Sorry, the content you are looking for does not exist.", html);
    }
}
