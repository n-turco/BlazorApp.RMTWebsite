using System.Net;
using System.Text.RegularExpressions;

namespace BlazorApp.RMTWebsite.Tests.Integration;

/// <summary>
/// The Services page now reads its price table from IServiceCatalog (plan 2.2). It must render
/// exactly what it showed when the prices were hard-coded, until the 2.5 redesign changes it on purpose.
/// </summary>
public class ServicesPageTests(SiteFactory factory) : IClassFixture<SiteFactory>
{
    // Same rows, same wording ("30 minute Massage", "$90.00") as the hard-coded table had
    [Fact]
    public async Task Get_Services_RendersEveryCatalogOption()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/services", TestContext.Current.CancellationToken);
        var body = Regex.Match(html, @"<tbody>(.*?)</tbody>", RegexOptions.Singleline).Groups[1].Value;
        var rows = Regex.Matches(body, @"<tr\b[^>]*>(.*?)</tr>", RegexOptions.Singleline)
            .Select(row => Regex.Matches(row.Groups[1].Value, @"<td\b[^>]*>(.*?)</td>", RegexOptions.Singleline)
                .Select(cell => WebUtility.HtmlDecode(cell.Groups[1].Value).Trim())
                .ToArray())
            .ToList();

        // Assert
        Assert.Equal(
            [
                ["Swedish Massage", "30 minute Massage", "$90.00"],
                ["Swedish Massage", "45 minute Massage", "$110.00"],
                ["Swedish Massage", "60 minute Massage", "$130.00"],
                ["Swedish Massage", "75 minute Massage", "$145.00"],
                ["Swedish Massage", "90 minute Massage", "$160.00"],
            ],
            rows);
    }
}
