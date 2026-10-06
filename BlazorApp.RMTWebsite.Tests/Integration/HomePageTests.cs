using System.Net;
using System.Text.RegularExpressions;

namespace BlazorApp.RMTWebsite.Tests.Integration;

/// <summary>
/// Checks the Home page hero (plan task 2.2, PR 1) in the real rendered HTML. The roadmap's goal:
/// a first-time visitor can tell what the business is, where it is, and how to book without
/// scrolling, so the headline, location, and both buttons must all be in the hero.
/// </summary>
public class HomePageTests(SiteFactory factory) : IClassFixture<SiteFactory>
{
    private const string Headline = "Therapeutic massage for recovery, pain relief, and performance";

    /// <summary>Fetches the Home page HTML through the in-memory site.</summary>
    private async Task<string> GetHomeAsync() =>
        await factory.CreateClient().GetStringAsync("/", TestContext.Current.CancellationToken);

    /// <summary>Returns the markup of the hero &lt;section&gt;, or an empty string if there isn't one.</summary>
    private static string GetHero(string html) =>
        Regex.Match(html, @"<section\b[^>]*\bclass=""[^""]*\bhero\b[^""]*""[^>]*>.*?</section>", RegexOptions.Singleline).Value;

    // "What": the visible headline is the page's only <h1> (it replaces the hidden stopgap from 2.4)
    [Fact]
    public async Task Get_Home_HasOneVisibleH1WithHeadline()
    {
        // Arrange & Act
        var html = await GetHomeAsync();
        var h1Tags = Regex.Matches(html, @"<h1\b([^>]*)>(.*?)</h1>", RegexOptions.Singleline);

        // Assert
        var h1 = Assert.Single(h1Tags);
        Assert.DoesNotContain("visually-hidden", h1.Groups[1].Value);
        Assert.Equal(Headline, WebUtility.HtmlDecode(h1.Groups[2].Value).Trim());
    }

    // "Where": the hero names the town, so visitors know straight away whether the clinic is local
    [Fact]
    public async Task Get_Home_HeroNamesGuelph()
    {
        // Arrange & Act
        var hero = GetHero(await GetHomeAsync());

        // Assert
        Assert.Contains("Guelph, Ontario", hero);
    }

    // "How to book": both calls to action are in the hero, pointing at the right pages
    [Theory]
    [InlineData("contact", "Book an appointment")]
    [InlineData("services", "View services")]
    public async Task Get_Home_HeroHasCallToActionLink(string href, string text)
    {
        // Arrange & Act
        var hero = GetHero(await GetHomeAsync());

        // Assert
        Assert.Matches($@"<a\b[^>]*\bhref=""{href}""[^>]*>\s*{Regex.Escape(text)}\s*</a>", hero);
    }

    // The hero photo is decorative (alt=""), loads first (fetchpriority), reserves its space
    // (width/height), and actually exists: a broken path would just show a dark box
    [Fact]
    public async Task Get_Home_HeroImageIsDecorativePrioritisedAndServed()
    {
        // Arrange
        var client = factory.CreateClient();
        var hero = GetHero(await client.GetStringAsync("/", TestContext.Current.CancellationToken));

        // Act
        var img = Regex.Match(hero, @"<img\b[^>]*>").Value;
        var src = Regex.Match(img, @"\bsrc=""([^""]+)""").Groups[1].Value;
        var image = await client.GetAsync("/" + src.TrimStart('/'), TestContext.Current.CancellationToken);

        // Assert
        // Blazor renders alt="" as a bare `alt` attribute; in HTML both mean an empty (decorative) alt
        Assert.Matches(@"\balt(=""""|(?=[\s/>]))", img);
        Assert.Matches(@"\bfetchpriority=""high""", img);
        Assert.Matches(@"\bwidth=""\d+""", img);
        Assert.Matches(@"\bheight=""\d+""", img);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/jpeg", image.Content.Headers.ContentType?.MediaType);
    }
}
