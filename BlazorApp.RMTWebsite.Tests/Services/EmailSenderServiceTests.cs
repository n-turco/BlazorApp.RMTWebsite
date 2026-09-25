using System.Net;
using System.Text;
using BlazorApp.RMTWebsite.Services;
using BlazorApp.RMTWebsite.Tests.Fakes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace BlazorApp.RMTWebsite.Tests.Services;

/// <summary>
/// Unit tests for <see cref="EmailSenderService"/>. Every test swaps the real network for
/// <see cref="FakeHttpMessageHandler"/> and uses made-up settings, so no Mailgun account,
/// API key, or internet connection is needed.
/// </summary>
public class EmailSenderServiceTests
{
    // Obviously fake values. Real keys must never appear in tests (see CLAUDE.md).
    private const string TestApiKey = "test-api-key";
    private const string TestBaseUrl = "https://api.mailgun.test/v3/example.test/messages";
    private const string TestToEmail = "therapist@example.test";

    /// <summary>
    /// Builds an in-memory configuration, the same shape as appsettings.json plus user secrets.
    /// Tests can pass overrides, e.g. a null value to simulate a missing setting.
    /// </summary>
    private static IConfiguration BuildConfig(Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Mailgun:ApiKey"] = TestApiKey,
            ["Mailgun:BaseUrl"] = TestBaseUrl,
            ["Mailgun:ToEmail"] = TestToEmail,
        };

        foreach (var (key, value) in overrides ?? [])
        {
            settings[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    /// <summary>Creates the service wired to the fake handler instead of the real network.</summary>
    private static EmailSenderService CreateService(FakeHttpMessageHandler handler, IConfiguration? config = null)
        => new(new HttpClient(handler), config ?? BuildConfig());

    [Fact]
    public async Task SendEmailAsync_ValidConfig_PostsToMailgunBaseUrl()
    {
        // Arrange: set up the fake network and the service under test.
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(handler);

        // Act: do the one thing this test is about.
        await service.SendEmailAsync("visitor@example.test", "Hello", "Test message");

        // Assert: check the outcome. The request must be a POST to the configured URL.
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal(TestBaseUrl, handler.LastRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task SendEmailAsync_ValidConfig_UsesBasicAuthWithApiKey()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(handler);

        // Mailgun expects HTTP Basic auth with the user name "api" and the key as the password,
        // Base64-encoded as "api:<key>".
        var expectedToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{TestApiKey}"));

        // Act
        await service.SendEmailAsync("visitor@example.test", "Hello", "Test message");

        // Assert
        var auth = handler.LastRequest?.Headers.Authorization;
        Assert.NotNull(auth);
        Assert.Equal("Basic", auth.Scheme);
        Assert.Equal(expectedToken, auth.Parameter);
    }

    [Fact]
    public async Task SendEmailAsync_ValidConfig_SendsRecipientSubjectAndMessage()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(handler);

        // Act
        await service.SendEmailAsync("visitor@example.test", "Booking question", "Do you have Saturday times?");

        // Assert: the body is URL-encoded form data ("to=...&subject=..."), so parse it into fields.
        Assert.NotNull(handler.LastRequestBody);
        var form = QueryHelpers.ParseQuery(handler.LastRequestBody);

        Assert.Equal(TestToEmail, form["to"]);
        Assert.Equal("Booking question", form["subject"]);
        Assert.Equal("Do you have Saturday times?", form["text"]);
    }

    // [Theory] runs the same test once per [InlineData] row, here once per required setting.
    [Theory]
    [InlineData("Mailgun:ApiKey")]
    [InlineData("Mailgun:BaseUrl")]
    public async Task SendEmailAsync_MissingRequiredSetting_ThrowsWithoutSending(string missingKey)
    {
        // Arrange: remove one required setting.
        var handler = new FakeHttpMessageHandler();
        var config = BuildConfig(new() { [missingKey] = null });
        var service = CreateService(handler, config);

        // Act + Assert: the call must fail with a clear error...
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendEmailAsync("visitor@example.test", "Hello", "Test message"));

        // ...and nothing should have been sent over the network.
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task SendEmailAsync_MailgunReturnsError_Throws()
    {
        // Arrange: make the fake reply the way Mailgun does when the key is wrong.
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "Forbidden");
        var service = CreateService(handler);

        // Act + Assert: the error must reach the caller, so the contact page can show
        // "Message failed to send" instead of falsely saying it succeeded.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendEmailAsync("visitor@example.test", "Hello", "Test message"));
    }
}
