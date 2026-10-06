using System.Globalization;
using BlazorApp.RMTWebsite.Models;

namespace BlazorApp.RMTWebsite.Tests.Models;

/// <summary>
/// Unit tests for <see cref="ServiceOffering"/> and <see cref="ServiceOption"/>: the "from $X"
/// starting price and the price format shown on every page (plan 2.2).
/// </summary>
public class ServiceOfferingTests
{
    // Options deliberately out of order, so "first" and "lowest" give different answers
    // (description and "helps with" don't affect these calculations, so they're placeholders)
    private static ServiceOffering CreateOffering() => new("Test Massage", "Test description.", ["Test benefit"],
    [
        new ServiceOption(60, 130m),
        new ServiceOption(30, 90m),
        new ServiceOption(90, 160m),
    ]);

    // "from $X" must be the cheapest option, not whichever happens to be listed first
    [Fact]
    public void StartingPrice_SeveralOptions_ReturnsLowest()
    {
        // Arrange
        var offering = CreateOffering();

        // Act
        var startingPrice = offering.StartingPrice;

        // Assert
        Assert.Equal(90m, startingPrice);
    }

    // "30 to 90 minutes" comes from the shortest and longest options
    [Fact]
    public void MinAndMaxMinutes_SeveralOptions_ReturnShortestAndLongest()
    {
        // Arrange
        var offering = CreateOffering();

        // Act
        var (min, max) = (offering.MinMinutes, offering.MaxMinutes);

        // Assert
        Assert.Equal(30, min);
        Assert.Equal(90, max);
    }

    // The server's culture must not change how prices look: a host set to French would otherwise
    // show "90,00 €". CurrentCulture is per async flow, so changing it here doesn't affect other tests.
    [Theory]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void FormattedPrice_AnyServerCulture_IsCanadianDollars(string serverCulture)
    {
        // Arrange
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(serverCulture);
        var option = new ServiceOption(30, 90m);

        try
        {
            // Act
            var formatted = option.FormattedPrice;

            // Assert
            Assert.Equal("$90.00", formatted);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
