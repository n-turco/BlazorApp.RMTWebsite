using System.Text.RegularExpressions;
using BlazorApp.RMTWebsite.Tests.Helpers;

namespace BlazorApp.RMTWebsite.Tests.Layout;

/// <summary>
/// Enforces the CLAUDE.md rule "no inline style attributes" (plan task 2.1). Inline styles can't be
/// reused, are hard to find, and beat every class, so styles belong in app.css, a scoped .razor.css
/// file, or a Bootstrap class. Reads the .razor source files copied into SiteFiles/ (see the .csproj).
/// </summary>
public class InlineStyleTests
{
    /// <summary>
    /// Every .razor file under Components/, as a path such as "Pages/Home.razor". Each one becomes a
    /// separate test case, so a failure names the file.
    /// </summary>
    public static TheoryData<string> RazorFiles()
    {
        var files = new TheoryData<string>();
        foreach (var file in Directory.GetFiles(RazorSource.ComponentsPath, "*.razor", SearchOption.AllDirectories))
        {
            files.Add(Path.GetRelativePath(RazorSource.ComponentsPath, file).Replace('\\', '/'));
        }
        return files;
    }

    // Matches style="..." or style='...' on any element; the (?<![\w-]) lookbehind skips data-style= etc.
    [Theory]
    [MemberData(nameof(RazorFiles))]
    public void Component_RazorFile_HasNoInlineStyleAttribute(string razorFile)
    {
        // Arrange
        var markup = RazorSource.ReadWithoutComments(razorFile);

        // Act
        var inlineStyles = Regex.Matches(markup, @"(?<![\w-])style\s*=\s*[""']", RegexOptions.IgnoreCase)
            .Select(match => match.Value)
            .ToList();

        // Assert
        Assert.Empty(inlineStyles);
    }
}
