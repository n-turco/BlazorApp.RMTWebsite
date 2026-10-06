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

    // Every Services card needs words to show: a blank description or empty "Helps with" list would
    // render an empty paragraph or list on the page (plan 2.5)
    [Fact]
    public void GetAll_EveryOffering_HasDescriptionAndHelpsWith()
    {
        // Arrange
        var catalog = new ServiceCatalog();

        // Act
        var offerings = catalog.GetAll();

        // Assert
        Assert.All(offerings, offering =>
        {
            Assert.False(string.IsNullOrWhiteSpace(offering.Description), $"{offering.Name} has no description.");
            Assert.NotEmpty(offering.HelpsWith);
            Assert.All(offering.HelpsWith, item => Assert.False(string.IsNullOrWhiteSpace(item)));
        });
    }

    // The copy approved on 2026-10-06, pinned like the prices so a wording change is deliberate
    [Fact]
    public void GetAll_SwedishMassage_HasApprovedCopy()
    {
        // Arrange
        var catalog = new ServiceCatalog();

        // Act
        var offering = Assert.Single(catalog.GetAll());

        // Assert
        Assert.Equal(
            "Swedish massage uses long, flowing strokes, kneading, and circular movements to work the muscles " +
            "and soft tissues. Pressure is matched to you, from gentle and relaxing to firmer, deeper-tissue work " +
            "on areas of tension, and each session is shaped by your health history and goals.",
            offering.Description);
        Assert.Equal(
            ["Muscle tension and stiffness",
             "Stress and trouble relaxing",
             "Recovery from training and everyday aches",
             "Ongoing (chronic) muscle pain, as part of a treatment plan"],
            offering.HelpsWith);
    }
}
