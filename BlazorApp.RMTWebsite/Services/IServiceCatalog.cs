using BlazorApp.RMTWebsite.Models;

namespace BlazorApp.RMTWebsite.Services;

/// <summary>
/// The one place that knows which treatments the clinic offers and what they cost (plan 2.2).
/// Pages ask this interface for data instead of hard-coding prices, so the Home preview and the
/// Services page can never disagree. A later version (plan 4.3) can read from configuration
/// without any page changing.
/// </summary>
public interface IServiceCatalog
{
    /// <summary>Every treatment, in the order the site should list them.</summary>
    IReadOnlyList<ServiceOffering> GetAll();
}
