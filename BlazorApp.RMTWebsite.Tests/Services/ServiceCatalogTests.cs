using BlazorApp.RMTWebsite.Models;
using BlazorApp.RMTWebsite.Services;

namespace BlazorApp.RMTWebsite.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ServiceCatalog"/>. These pin today's published prices, so a price can
/// only change on purpose: whoever changes one has to update this test in the same commit.
/// </summary>
public class ServiceCatalogTests
{
    // The prices that were on the Services page before the catalog existed
    [Fact]
    public void GetAll_SwedishMassage_HasTodaysFivePrices()
    {
        // Arrange
        var catalog = new ServiceCatalog();

        // Act
        var offering = Assert.Single(catalog.GetAll());

        // Assert
        Assert.Equal("Swedish Massage", offering.Name);
        Assert.Equal(
            [new ServiceOption(30, 90m), new ServiceOption(45, 110m), new ServiceOption(60, 130m),
             new ServiceOption(75, 145m), new ServiceOption(90, 160m)],
            offering.Options);
    }
}
