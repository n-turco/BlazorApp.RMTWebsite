using System.Text.RegularExpressions;
using BlazorApp.RMTWebsite.Tests.Helpers;

namespace BlazorApp.RMTWebsite.Tests.Layout;

/// <summary>
/// Checks the top navbar and page skeleton (plan task 2.3): a real, labelled menu button, no inline
/// JavaScript, no sidebar, a skip link, and nav links that match the footer. Reads the .razor source
/// files copied into SiteFiles/ (see the .csproj). Behaviour in the browser (opening the menu, closing
/// it after navigation) is checked by hand; Playwright can automate that in 6.2 PR 3.
/// </summary>
public class NavMenuTests
{
    private static string NavMenu => RazorSource.ReadWithoutComments(Path.Combine("Layout", "NavMenu.razor"));

    private static string MainLayout => RazorSource.ReadWithoutComments(Path.Combine("Layout", "MainLayout.razor"));

    /// <summary>Returns the value of an attribute inside one tag, or null if it isn't there.</summary>
    private static string? Attribute(string tag, string name)
    {
        var match = Regex.Match(tag, $@"\b{name}=""([^""]*)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    // A checkbox can't act as a menu button for screen readers; a <button> with these attributes can
    [Fact]
    public void NavMenu_Toggler_IsButtonWithAriaControlsAndExpanded()
    {
        // Arrange
        var markup = NavMenu;

        // Act
        var toggler = Regex.Match(markup, @"<button\b[^>]*\bnavbar-toggler\b[^>]*>").Value;
        var controls = Attribute(toggler, "aria-controls");

        // Assert
        Assert.NotEmpty(toggler);
        Assert.Equal("false", Attribute(toggler, "aria-expanded"));
        Assert.False(string.IsNullOrWhiteSpace(Attribute(toggler, "aria-label")), "The icon-only button needs an aria-label.");
        Assert.NotNull(controls);
        Assert.Matches($@"\bid=""{Regex.Escape(controls)}""", markup);
    }

    // Inline handlers (onclick="...") mix JavaScript into markup and would be blocked by a future CSP
    [Fact]
    public void NavMenu_Markup_HasNoInlineEventHandlers()
    {
        // Arrange
        var markup = NavMenu;

        // Act: matches onclick=, onchange=, ... but not Blazor's @onclick (not usable in static SSR anyway)
        var handlers = Regex.Matches(markup, @"(?<![\w@-])on[a-z]+\s*=", RegexOptions.IgnoreCase)
            .Select(match => match.Value)
            .ToList();

        // Assert
        Assert.Empty(handlers);
    }

    // Both lists name the site's pages; if one gains a page the other must too
    [Fact]
    public void NavMenu_Links_MatchFooterQuickLinks()
    {
        // Arrange
        var footer = RazorSource.ReadWithoutComments(Path.Combine("Layout", "SiteFooter.razor"));
        var quickLinks = Regex.Match(footer, @"aria-label=""Footer"".*?</nav>", RegexOptions.Singleline).Value;

        // Act
        var navHrefs = Regex.Matches(NavMenu, @"<NavLink\b[^>]*\bhref=""([^""]*)""").Select(m => m.Groups[1].Value).ToList();
        var footerHrefs = Regex.Matches(quickLinks, @"<a\b[^>]*\bhref=""([^""]*)""").Select(m => m.Groups[1].Value).ToList();

        // Assert
        Assert.NotEmpty(navHrefs);
        Assert.Equal(footerHrefs, navHrefs);
    }

    // The 250px sidebar is replaced by the top navbar
    [Fact]
    public void MainLayout_Markup_HasNoSidebar()
    {
        // Arrange
        var markup = MainLayout;

        // Act
        var hasSidebar = markup.Contains("sidebar");

        // Assert
        Assert.False(hasSidebar);
    }

    // Keyboard users can jump past the nav (WCAG 2.4.1). It must come before the nav, and point at <main>
    [Fact]
    public void MainLayout_SkipLink_ComesFirstAndTargetsMain()
    {
        // Arrange
        var markup = MainLayout;

        // Act
        var skipLink = Regex.Match(markup, @"<a\b[^>]*\bvisually-hidden-focusable\b[^>]*>");
        var navMenu = markup.IndexOf("<NavMenu", StringComparison.Ordinal);
        var main = Regex.Match(markup, @"<main\b[^>]*>").Value;

        // The href is computed (current path + fragment, because of <base href="/">), so check the
        // property it points at builds a "#main-content" link
        var hrefExpression = Attribute(skipLink.Value, "href")?.TrimStart('@') ?? "";
        var hrefProperty = Regex.Match(markup, $@"\b{Regex.Escape(hrefExpression)}\s*=>[^;]*;", RegexOptions.Singleline).Value;

        // Assert
        Assert.True(skipLink.Success, "MainLayout needs a visually-hidden-focusable skip link.");
        Assert.True(skipLink.Index < navMenu, "The skip link must come before the nav.");
        Assert.Contains("#main-content", hrefProperty);
        Assert.Equal("main-content", Attribute(main, "id"));
    }

    // Bootstrap's collapse powers the menu button; it must load before Blazor starts patching pages
    [Fact]
    public void AppRazor_Scripts_LoadBootstrapBeforeBlazor()
    {
        // Arrange
        var appRazor = RazorSource.ReadWithoutComments("App.razor");

        // Act
        var bootstrap = appRazor.IndexOf("bootstrap/dist/js/bootstrap.min.js", StringComparison.Ordinal);
        var blazor = appRazor.IndexOf("_framework/blazor.web.js", StringComparison.Ordinal);

        // Assert
        Assert.True(bootstrap >= 0, "App.razor should load bootstrap.min.js.");
        Assert.True(bootstrap < blazor, "bootstrap.min.js should load before blazor.web.js.");
    }
}
