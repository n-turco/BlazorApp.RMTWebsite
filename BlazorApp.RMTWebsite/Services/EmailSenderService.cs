using BlazorApp.RMTWebsite.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace BlazorApp.RMTWebsite.Services
{
    public class EmailSenderService(IOptions<EmailSettings> settings) : IEmailSender
    {
        private readonly EmailSettings? _settings = settings.Value; 

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if(_settings != null)
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
                {
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                    EnableSsl = true
                };
                var mail = new MailMessage(_settings.SenderEmail, _settings.ToEmail, subject, htmlMessage);
                mail.ReplyToList.Add(email); // reply straight to the sender
                await client.SendMailAsync(mail);
            }

        }
    }
}
