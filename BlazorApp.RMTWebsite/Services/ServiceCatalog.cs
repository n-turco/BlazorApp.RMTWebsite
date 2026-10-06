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
        new ServiceOffering(
            Name: "Swedish Massage",
            // Copy approved 2026-10-06 (plan 2.5); ServiceCatalogTests pins it word for word
            Description:
                "Swedish massage uses long, flowing strokes, kneading, and circular movements to work the muscles " +
                "and soft tissues. Pressure is matched to you, from gentle and relaxing to firmer, deeper-tissue work " +
                "on areas of tension, and each session is shaped by your health history and goals.",
            HelpsWith:
            [
                "Muscle tension and stiffness",
                "Stress and trouble relaxing",
                "Recovery from training and everyday aches",
                "Ongoing (chronic) muscle pain, as part of a treatment plan",
            ],
            Options:
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
