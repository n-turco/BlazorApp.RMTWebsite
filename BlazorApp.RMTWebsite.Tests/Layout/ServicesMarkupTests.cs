using System.Text.RegularExpressions;
using BlazorApp.RMTWebsite.Tests.Helpers;

namespace BlazorApp.RMTWebsite.Tests.Layout;

/// <summary>
/// Source checks on Services.razor (plan 2.5, and the 2.8 "Done when"): spacing comes from CSS
/// (PageHeader and Services.razor.css), not from &lt;hr&gt; and &lt;br&gt; tags added for gaps.
/// </summary>
public class ServicesMarkupTests
{
    // <hr> means "change of topic" and <br> means "line break in text"; neither is a spacing tool
    [Fact]
    public void ServicesRazor_HasNoHrOrBrSpacing()
    {
        // Arrange
        var markup = RazorSource.ReadWithoutComments("Pages/Services.razor");

        // Act
        var spacingTags = Regex.Matches(markup, @"<(hr|br)\b[^>]*>", RegexOptions.IgnoreCase)
            .Select(match => match.Value)
            .ToList();

        // Assert
        Assert.Empty(spacingTags);
    }
}
