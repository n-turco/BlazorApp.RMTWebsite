using Microsoft.AspNetCore.Identity.UI.Services;

namespace BlazorApp.RMTWebsite.Tests.Fakes;

/// <summary>
/// Stands in for EmailSenderService in integration tests: records each email instead of calling
/// Mailgun, and can be told to fail so tests can check the contact form's error message.
/// </summary>
public class FakeEmailSender : IEmailSender
{
    /// <summary>Every email the site tried to send, in order.</summary>
    public List<(string Email, string Subject, string Message)> SentEmails { get; } = [];

    /// <summary>When true, SendEmailAsync throws, like EmailSenderService does when Mailgun fails.</summary>
    public bool ShouldFail { get; set; }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (ShouldFail)
        {
            throw new InvalidOperationException("Fake send failure.");
        }

        SentEmails.Add((email, subject, htmlMessage));
        return Task.CompletedTask;
    }

    /// <summary>Clears recorded emails and failure mode, so each test starts fresh.</summary>
    public void Reset()
    {
        SentEmails.Clear();
        ShouldFail = false;
    }
}
