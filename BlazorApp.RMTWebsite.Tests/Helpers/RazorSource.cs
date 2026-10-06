using System.Text.RegularExpressions;

namespace BlazorApp.RMTWebsite.Tests.Helpers;

/// <summary>
/// Shared helpers for tests that read the site's .razor source files. The test project copies
/// Components/**/*.razor into the output folder under SiteFiles/ (see the .csproj).
/// </summary>
public static class RazorSource
{
    /// <summary>The copy of the site's Components folder in the test output.</summary>
    public static readonly string ComponentsPath = Path.Combine(AppContext.BaseDirectory, "SiteFiles", "Components");

    /// <summary>
    /// Reads a .razor file (path relative to Components/, e.g. "Pages/FAQ.razor") with Razor (@* *@)
    /// and HTML (&lt;!-- --&gt;) comments removed. Comments never reach the browser, so they shouldn't
    /// count when checking the markup.
    /// </summary>
    public static string ReadWithoutComments(string relativePath)
    {
        var markup = File.ReadAllText(Path.Combine(ComponentsPath, relativePath));
        return Regex.Replace(markup, @"@\*.*?\*@|<!--.*?-->", "", RegexOptions.Singleline);
    }
}
