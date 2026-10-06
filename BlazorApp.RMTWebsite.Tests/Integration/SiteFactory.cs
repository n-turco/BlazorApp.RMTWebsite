using BlazorApp.RMTWebsite.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorApp.RMTWebsite.Tests.Integration;

/// <summary>
/// Starts the whole site in memory (plan 6.2 PR 2, ADR 0002). Requests go through the real
/// Program.cs pipeline: routing, antiforgery, Razor components. No network port is opened.
/// Three layers keep tests away from Mailgun:
///   1. Environment "Testing": .NET only loads user secrets (the real Mailgun key) in Development.
///   2. Obviously fake Mailgun settings, in case anything still reads them.
///   3. IEmailSender replaced by FakeEmailSender, which records emails instead of sending them.
/// </summary>
public class SiteFactory : WebApplicationFactory<Program>
{
    /// <summary>The fake the site uses for every email in these tests.</summary>
    public FakeEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mailgun:ApiKey"] = "test-api-key",
            ["Mailgun:BaseUrl"] = "http://mailgun.example.test/",
            ["Mailgun:ToEmail"] = "clinic@example.test",
        }));

        // ConfigureTestServices runs after Program.cs registers its services, so this replaces
        // the real EmailSenderService registration rather than adding a second one.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }
}
