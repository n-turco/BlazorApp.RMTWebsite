using BlazorApp.RMTWebsite.Models;

namespace BlazorApp.RMTWebsite.Services;

/// <summary>
/// In-memory list of the clinic's treatments and prices (plan 2.2). Changing a price here changes
/// it on every page; ServiceCatalogTests pins the current prices so a change is always deliberate.
/// </summary>
public class ServiceCatalog : IServiceCatalog
{
    // Built once and shared: the data never changes while the site is running (registered as a singleton)
    private static readonly IReadOnlyList<ServiceOffering> Offerings =
    [
        new ServiceOffering("Swedish Massage",
        [
            new ServiceOption(30, 90m),
            new ServiceOption(45, 110m),
            new ServiceOption(60, 130m),
            new ServiceOption(75, 145m),
            new ServiceOption(90, 160m),
        ]),
    ];

    public IReadOnlyList<ServiceOffering> GetAll() => Offerings;
}
