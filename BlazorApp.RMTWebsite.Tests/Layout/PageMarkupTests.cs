using System.Text.RegularExpressions;
using BlazorApp.RMTWebsite.Tests.Helpers;

namespace BlazorApp.RMTWebsite.Tests.Layout;

/// <summary>
/// Source checks on page markup (plans 2.5 and 2.8, the 2.8 "Done when"): spacing comes from CSS
/// (PageHeader and each page's .razor.css), not from &lt;hr&gt; and &lt;br&gt; tags added for gaps.
/// About and FAQ join this list in 2.6 and 2.7, when their bodies are rebuilt without line breaks.
/// </summary>
public class PageMarkupTests
{
    // <hr> means "change of topic" and <br> means "line break in text"; neither is a spacing tool
    [Theory]
    [InlineData("Services.razor")]
    [InlineData("ContactUs.razor")]
    [InlineData("NotFound.razor")]
    public void PageRazor_Markup_HasNoHrOrBrTags(string pageFile)
    {
        // Arrange
        var markup = RazorSource.ReadWithoutComments(Path.Combine("Pages", pageFile));

        // Act
        var spacingTags = Regex.Matches(markup, @"<(hr|br)\b[^>]*>", RegexOptions.IgnoreCase)
            .Select(match => match.Value)
            .ToList();

        // Assert
        Assert.Empty(spacingTags);
    }
}
