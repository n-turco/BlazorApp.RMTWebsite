using System.Globalization;

namespace BlazorApp.RMTWebsite.Models;

/// <summary>One bookable length of a treatment, e.g. 60 minutes for $130 (plan 2.2).</summary>
/// <param name="Minutes">Length of the appointment in minutes.</param>
/// <param name="Price">Price in Canadian dollars.</param>
public record ServiceOption(int Minutes, decimal Price)
{
    /// <summary>
    /// Canadian English formatting, fixed. "C" (currency) on its own would use the server's culture,
    /// so a host set to French would show "130,00 €".
    /// </summary>
    private static readonly CultureInfo PriceCulture = CultureInfo.GetCultureInfo("en-CA");

    /// <summary>Formats any price the way the whole site shows prices, e.g. "$130.00".</summary>
    public static string FormatPrice(decimal price) => price.ToString("C", PriceCulture);

    /// <summary>This option's price as shown on the site, e.g. "$130.00".</summary>
    public string FormattedPrice => FormatPrice(Price);
}

/// <summary>
/// A treatment the clinic offers: what it is, who it helps, and its bookable lengths and prices
/// (plan 2.2; description and "helps with" added for the Services cards in 2.5).
/// </summary>
/// <param name="Name">Treatment name, e.g. "Swedish Massage".</param>
/// <param name="Description">Two or three sentences describing the treatment, shown on its Services card.</param>
/// <param name="HelpsWith">Short points on who or what it helps, shown as a bulleted list.</param>
/// <param name="Options">Each length and its price.</param>
public record ServiceOffering(
    string Name,
    string Description,
    IReadOnlyList<string> HelpsWith,
    IReadOnlyList<ServiceOption> Options)
{
    /// <summary>The lowest price, shown as "from $X" on the Home page.</summary>
    public decimal StartingPrice => Options.Min(option => option.Price);

    /// <summary>The shortest appointment length in minutes.</summary>
    public int MinMinutes => Options.Min(option => option.Minutes);

    /// <summary>The longest appointment length in minutes.</summary>
    public int MaxMinutes => Options.Max(option => option.Minutes);

    /// <summary>The starting price formatted like every other price on the site, e.g. "$90.00".</summary>
    public string FormattedStartingPrice => ServiceOption.FormatPrice(StartingPrice);
}
