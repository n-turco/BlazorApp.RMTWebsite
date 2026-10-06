using System.Net;
using System.Text.RegularExpressions;
using BlazorApp.RMTWebsite.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorApp.RMTWebsite.Tests.Integration;

/// <summary>
/// Checks the redesigned Services page (plan task 2.5) in the real rendered HTML. The roadmap's
/// "Done when": each treatment is described and bookable from the page. Expected names and prices
/// come from the site's own IServiceCatalog, so these tests follow the catalog instead of copying it
/// (ServiceCatalogTests is where the actual prices and copy are pinned).
/// </summary>
public class ServicesPageTests(SiteFactory factory) : IClassFixture<SiteFactory>
{
    private const string Subtitle = "Registered massage therapy in Guelph, Ontario";
    private const string PricingNote = "Prices are plus HST. Official receipts are provided for extended health insurance claims.";

    /// <summary>Fetches the Services page HTML through the in-memory site.</summary>
    private async Task<string> GetServicesAsync() =>
        await factory.CreateClient().GetStringAsync("/services", TestContext.Current.CancellationToken);

    /// <summary>The same catalog the page reads, taken from the site's own DI container.</summary>
    private IServiceCatalog Catalog => factory.Services.GetRequiredService<IServiceCatalog>();

    /// <summary>Every treatment card (&lt;article&gt;) on the page, in order.</summary>
    private static List<string> GetCards(string html) =>
        Regex.Matches(html, @"<article\b[^>]*>.*?</article>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .ToList();

    /// <summary>Strips tags and decodes entities, e.g. "&lt;td&gt;$90.00&lt;/td&gt;" becomes "$90.00".</summary>
    private static string Text(string markup) =>
        WebUtility.HtmlDecode(Regex.Replace(markup, "<[^>]+>", "")).Trim();

    // PageHeader renders the page's only <h1> and the subtitle together in one header block
    [Fact]
    public async Task Get_Services_HasPageHeaderH1AndSubtitle()
    {
        // Arrange & Act
        var html = await GetServicesAsync();
        var header = Regex.Match(html, @"<header\b[^>]*\bclass=""[^""]*\bpage-header\b[^""]*""[^>]*>.*?</header>", RegexOptions.Singleline).Value;
        var h1Tags = Regex.Matches(html, @"<h1\b[^>]*>(.*?)</h1>", RegexOptions.Singleline);

        // Assert
        var h1 = Assert.Single(h1Tags);
        Assert.Equal("Services and rates", Text(h1.Groups[1].Value));
        Assert.Contains(h1.Value, header);
        Assert.Contains(Subtitle, header);
    }

    // One card per treatment (not per length), each named by its own h2 so screen readers can list
    // the cards by name, with a description paragraph and a "Helps with" list
    [Fact]
    public async Task Get_Services_RendersCardPerCatalogOffering()
    {
        // Arrange
        var offerings = Catalog.GetAll();

        // Act
        var cards = GetCards(await GetServicesAsync());

        // Assert
        Assert.Equal(offerings.Count, cards.Count);
        foreach (var (offering, card) in offerings.Zip(cards))
        {
            var labelledBy = Regex.Match(card, @"^<article\b[^>]*\baria-labelledby=""([^""]+)""").Groups[1].Value;
            Assert.NotEmpty(labelledBy);
            Assert.Matches($@"<h2\b[^>]*\bid=""{Regex.Escape(labelledBy)}""[^>]*>\s*{Regex.Escape(offering.Name)}\s*</h2>", card);
            Assert.Matches(@"<p\b[^>]*>\s*\S[^<]*</p>", card);
            Assert.Matches(@"<ul\b[^>]*>\s*<li\b", card);
        }
    }

    // Prices are a real table (Length, Price) with a caption naming the treatment, wrapped in
    // table-responsive; one row per length, e.g. "30 minutes" / "$90.00", straight from the catalog
    [Fact]
    public async Task Get_Services_CardPriceTableMatchesCatalog()
    {
        // Arrange
        var offerings = Catalog.GetAll();

        // Act
        var cards = GetCards(await GetServicesAsync());

        // Assert
        Assert.Equal(offerings.Count, cards.Count);
        foreach (var (offering, card) in offerings.Zip(cards))
        {
            Assert.Matches(@"<div\b[^>]*\bclass=""[^""]*\btable-responsive\b[^""]*""[^>]*>\s*<table\b", card);
            var caption = Regex.Match(card, @"<caption\b[^>]*>(.*?)</caption>", RegexOptions.Singleline).Groups[1].Value;
            Assert.Equal($"{offering.Name} prices", Text(caption));

            var headers = Regex.Matches(Regex.Match(card, @"<thead\b[^>]*>(.*?)</thead>", RegexOptions.Singleline).Value, @"<th\b[^>]*>(.*?)</th>", RegexOptions.Singleline)
                .Select(m => Text(m.Groups[1].Value));
            Assert.Equal(["Length", "Price"], headers);

            // Each row is a <th scope="row"> (the length) and a <td> (the price). The tag patterns allow
            // attributes because scoped CSS stamps every element, e.g. <tbody b-x7k2q9>
            var body = Regex.Match(card, @"<tbody\b[^>]*>(.*?)</tbody>", RegexOptions.Singleline).Groups[1].Value;
            var rows = Regex.Matches(body, @"<tr\b[^>]*>(.*?)</tr>", RegexOptions.Singleline)
                .Select(row => Regex.Matches(row.Groups[1].Value, @"<t[hd]\b[^>]*>(.*?)</t[hd]>", RegexOptions.Singleline)
                    .Select(cell => Text(cell.Groups[1].Value))
                    .ToArray())
                .ToList();
            Assert.Equal(
                offering.Options.Select(option => new[] { $"{option.Minutes} minutes", option.FormattedPrice }).ToList(),
                rows);
            Assert.Equal(offering.Options.Count, Regex.Matches(body, @"<th\b[^>]*\bscope=""row""").Count);
        }
    }

    // Every card is bookable from the page. btn-primary (white on brand blue) because the cyan accent
    // button's edge is too faint on a white card (WCAG 1.4.11 needs 3:1 against the background)
    [Fact]
    public async Task Get_Services_CardHasBookButtonToContact()
    {
        // Arrange & Act
        var cards = GetCards(await GetServicesAsync());

        // Assert
        Assert.NotEmpty(cards);
        Assert.All(cards, card =>
        {
            var button = Regex.Match(card, @"<a\b[^>]*\bhref=""contact""[^>]*>\s*Book this treatment\s*</a>").Value;
            Assert.NotEmpty(button);
            Assert.Matches(@"\bclass=""[^""]*\bbtn-primary\b", button);
        });
    }

    // The confirmed facts (2026-10-06): prices are plus HST, and insurance receipts are provided
    [Fact]
    public async Task Get_Services_ShowsPricingNote()
    {
        // Arrange & Act
        var html = await GetServicesAsync();

        // Assert
        Assert.Contains(PricingNote, Text(html));
    }

    // The shared CallToAction band closes the page, after the last treatment card
    [Fact]
    public async Task Get_Services_EndsWithCallToAction()
    {
        // Arrange & Act
        var html = await GetServicesAsync();
        var lastCardEnd = html.LastIndexOf("</article>", StringComparison.Ordinal);
        var cta = Regex.Match(html, @"<section\b[^>]*\bclass=""cta""[^>]*>.*?</section>", RegexOptions.Singleline);

        // Assert
        Assert.True(lastCardEnd >= 0, "No treatment card found.");
        Assert.True(cta.Success, "No CallToAction section found.");
        Assert.True(cta.Index > lastCardEnd, "CallToAction should come after the treatment cards.");
        Assert.Matches(@"<a\b[^>]*\bhref=""contact""", cta.Value);
    }
}
