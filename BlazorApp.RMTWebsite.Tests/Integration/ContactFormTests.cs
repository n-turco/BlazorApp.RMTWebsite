using System.Net;
using System.Text.RegularExpressions;

namespace BlazorApp.RMTWebsite.Tests.Integration;

/// <summary>
/// Submits the contact form the way a browser does: GET the page (which sets the antiforgery
/// cookie and hidden token), then POST the fields back. The site's IEmailSender is a
/// FakeEmailSender (see SiteFactory), so nothing is sent to Mailgun.
/// </summary>
public class ContactFormTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public ContactFormTests(SiteFactory factory)
    {
        _factory = factory;
        // The fake is shared by every test in this class; start each one with no recorded emails.
        _factory.EmailSender.Reset();
    }

    /// <summary>
    /// Loads /contact and posts the form with the given fields, plus the hidden fields the page
    /// rendered (antiforgery token and form name). The client keeps the antiforgery cookie between
    /// the two requests. Returns the HTML of the page shown after the POST.
    /// </summary>
    private async Task<string> SubmitContactFormAsync(string email, string subject, string message)
    {
        var client = _factory.CreateClient();
        var page = await client.GetStringAsync("/contact", TestContext.Current.CancellationToken);

        var fields = Regex.Matches(page, @"<input type=""hidden"" name=""([^""]+)"" value=""([^""]*)""")
            .ToDictionary(m => m.Groups[1].Value, m => WebUtility.HtmlDecode(m.Groups[2].Value));
        fields["Inquiry.EmailAddress"] = email;
        fields["Inquiry.EmailSubject"] = subject;
        fields["Inquiry.EmailContent"] = message;

        var response = await client.PostAsync("/contact", new FormUrlEncodedContent(fields), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    // The happy path: one email goes out with exactly what the visitor typed
    [Fact]
    public async Task Post_ValidForm_SendsOneEmailAndShowsSuccess()
    {
        // Arrange & Act
        var html = await SubmitContactFormAsync("visitor@example.test", "Booking question", "Do you have Saturday times?");

        // Assert
        var sent = Assert.Single(_factory.EmailSender.SentEmails);
        Assert.Equal("visitor@example.test", sent.Email);
        Assert.Equal("Booking question", sent.Subject);
        Assert.Equal("Do you have Saturday times?", sent.Message);
        Assert.Contains("Message successfully sent.", html);
    }

    // Server-side validation (DataAnnotations) must stop the send, not just the browser
    [Fact]
    public async Task Post_MissingEmailAddress_ShowsValidationMessageAndSendsNothing()
    {
        // Arrange & Act
        var html = await SubmitContactFormAsync("", "Booking question", "Do you have Saturday times?");

        // Assert
        Assert.Empty(_factory.EmailSender.SentEmails);
        Assert.Contains("Email Address is required.", html);
    }

    // When Mailgun fails, the visitor sees an error instead of a false "sent"
    [Fact]
    public async Task Post_SenderThrows_ShowsFailureMessage()
    {
        // Arrange
        _factory.EmailSender.ShouldFail = true;

        // Act
        var html = await SubmitContactFormAsync("visitor@example.test", "Booking question", "Do you have Saturday times?");

        // Assert
        Assert.Contains("Something went wrong. Message failed to send.", html);
        Assert.DoesNotContain("Message successfully sent.", html);
    }
}
