using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;


namespace BlazorApp.RMTWebsite.Services
{
    public class EmailSenderService(HttpClient httpClient, IConfiguration config, ILogger<EmailSenderService> logger) : IEmailSender
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _config = config;

        // Sends contact form message to the site owner through Mailgun HTTP API
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var apiKey = _config["Mailgun:ApiKey"];
            var requestUrl = _config["Mailgun:BaseUrl"];
            var toEmail = _config["Mailgun:ToEmail"];

            // Validate that the required configuration values are present
            if (string.IsNullOrWhiteSpace(requestUrl)
                || string.IsNullOrWhiteSpace(apiKey)
                || string.IsNullOrWhiteSpace(toEmail))
            {
                var missingKeys = new List<string>();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    missingKeys.Add("Mailgun:ApiKey");
                }

                if (string.IsNullOrWhiteSpace(requestUrl))
                {
                    missingKeys.Add("Mailgun:BaseUrl");
                }

                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    missingKeys.Add("Mailgun:ToEmail");
                }

                logger.LogError("Mailgun configuration is missing required settings: {MissingKeys}", missingKeys);
                throw new InvalidOperationException("Mailgun configuration is missing");
            }

            // Validate the email address format
            if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
            {
                throw new ArgumentException("Visitor email address is invalid.", nameof(email));
            }

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "from", "postmaster@sandbox29ee27561c3e452ea58e840ba561dcb8.mailgun.org" },
                { "to", toEmail },
                { "subject", subject },
                { "text", $"From: {email}\n\n{htmlMessage}" },
                { "h:Reply-To", email }
            });

            request.Content = content;

            // Send to Mailgun; log the outcome without the response body or visitor details.
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Contact email failed to send. Mailgun returned status {StatusCode}.", (int)response.StatusCode);
                throw new InvalidOperationException($"Mailgun returned status {(int)response.StatusCode}.");
            }

            logger.LogInformation("Contact email successfully sent. Mailgun returned status {StatusCode}.", (int)response.StatusCode);
        }
    }
}
