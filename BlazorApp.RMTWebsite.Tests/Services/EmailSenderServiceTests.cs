using System.Net;
using System.Text;
using BlazorApp.RMTWebsite.Services;
using BlazorApp.RMTWebsite.Tests.Fakes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
    private static EmailSenderService CreateService(FakeHttpMessageHandler handler, IConfiguration? config = null, FakeLogger<EmailSenderService>? logger = null)
        => new(new HttpClient(handler), config ?? BuildConfig(), logger ?? new FakeLogger<EmailSenderService>());

    // This test checks that the service posts to the configured Mailgun URL.
    [Fact]
    public async Task SendEmailAsync_ValidConfig_PostsToMailgunBaseUrl()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(handler);

        // Act
        await service.SendEmailAsync("visitor@example.test", "Hello", "Test message");

        // Assert
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal(TestBaseUrl, handler.LastRequest.RequestUri?.ToString());
    }

    // This test checks that the service uses HTTP Basic auth with the API key, as Mailgun requires.
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

    // This test checks that the visitor's email, subject, and message are all included in the request body.
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
        Assert.Equal("From: visitor@example.test\n\nDo you have Saturday times?", form["text"]);
    }

    // [Theory] runs the same test once per [InlineData] row, here once per required setting.
    [Theory]
    [InlineData("Mailgun:ApiKey")]
    [InlineData("Mailgun:BaseUrl")]
    [InlineData("Mailgun:ToEmail")]
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

    // This test simulates Mailgun returning an error, e.g. because the API key is wrong.
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

    // This test checks that the visitor's email is included in the body of the message.
    [Fact]
    public async Task SendEmailAsync_ValidConfig_SetsReplyToVisitorEmail()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(handler);

        // Act
        await service.SendEmailAsync("visitor@example.test", "Booking question", "Do you have Saturday times?");

        // Assert
        var form = QueryHelpers.ParseQuery(handler.LastRequestBody);
        Assert.Equal("visitor@example.test", form["h:Reply-To"]);
    }

    // This test explicitly checks that the email body starts with the visitor's email.
    [Fact]
    public async Task SendEmailAsync_ValidConfig_StartsBodyWithVisitorEmail()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(handler);

        // Act
        await service.SendEmailAsync("visitor@example.test", "Booking question", "Do you have Saturday times?");

        // Assert
        var form = QueryHelpers.ParseQuery(handler.LastRequestBody);
        Assert.StartsWith("From: visitor@example.test", form["text"].ToString());
    }

    // [Theory] runs the same test once per [InlineData] row, here once per invalid email.
    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("Visitor@example.test\r\nBcc: victim@example.test")]
    [InlineData("Visitor <visitor@example.test>")]
    public async Task SendEmailAsync_InvalidVisitorEmail_ThrowsWithoutSending(string visitorEmail)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(handler);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.SendEmailAsync(visitorEmail, "Booking question", "Do you have Saturday times?"));
        Assert.Null(handler.LastRequest);
    }

    // This test checks that a failed send is logged as an Error that includes Mailgun's status code.
    [Fact]
    public async Task SendEmailAsync_MailgunReturnsError_LogsErrorWithStatusCode()
    {
        // Arrange: Mailgun replies 401 Unauthorized, and we keep a reference to the logger to inspect it.
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "Forbidden");
        var logger = new FakeLogger<EmailSenderService>();
        var service = CreateService(handler, logger: logger);

        // Act: the service still throws, so catch it; this test only checks the logs.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendEmailAsync("visitor@example.test", "Hello", "Test message"));

        // Assert: exactly one Error entry, and it mentions the numeric status code (401).
        var entry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains("401", entry.Message);
    }

    // This test checks that if Mailgun returns an error, the logger does not log the response body, which could contain sensitive information.
    [Fact]
    public async Task SendEmailAsync_MailgunReturnsError_DoesNotLogResponseBody()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "secret-response-body");
        var logger = new FakeLogger<EmailSenderService>();
        var service = CreateService(handler, logger: logger);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendEmailAsync("visitor@example.test", "Hello", "Test Message"));

        // Assert
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("secret-response-body"));
    }

    // This test checks that if Mailgun returns an error, the exception thrown does not include the response body, which could contain sensitive information.
    [Fact]
    public async Task SendEmailAsync_MailgunReturnsError_ExceptionOmitsResponseBody()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "secret-response-body");
        var service = CreateService(handler);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendEmailAsync("visitor@example.test", "Hello", "Test Message"));

        // Assert
        Assert.DoesNotContain("secret-response-body", ex.Message);
    }

    // This test checks that a successful send is logged as Information, but without including the visitor's email, subject, or message.
    [Fact]
    public async Task SendEmailAsync_MailgunValidConfig_LogsInformationWithoutVisitorDetails()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var logger = new FakeLogger<EmailSenderService>();
        var service = CreateService(handler, logger: logger);

        // Act
        await service.SendEmailAsync("visitor@example.test", "Booking question", "Do you have Saturday times?");

        // Assert
        Assert.Single(logger.Entries, e => e.Level == LogLevel.Information);
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("visitor@example.test"));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("Booking question"));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("Saturday"));
    }

    // This test checks that if a required setting is missing, the service logs an Error that names the missing key, but does not log the API key's value.
    [Theory]
    [InlineData("Mailgun:ApiKey")]
    [InlineData("Mailgun:BaseUrl")]
    [InlineData("Mailgun:ToEmail")]
    public async Task SendEmailAsync_MailgunMissingRequiredSetting_LogsError(string missingKey)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var config = BuildConfig(new() { [missingKey] = null });
        var logger = new FakeLogger<EmailSenderService>();
        var service = CreateService(handler, config, logger);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendEmailAsync("visitor@example.test", "", ""));

        // Assert: one Error entry that names the missing key, without leaking the API key's value.
        var entry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains(missingKey, entry.Message);
        Assert.DoesNotContain(TestApiKey, entry.Message);
    }
}
