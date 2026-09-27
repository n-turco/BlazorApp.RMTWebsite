using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;


namespace BlazorApp.RMTWebsite.Services
{
    public class EmailSenderService(HttpClient httpClient, IConfiguration config) : IEmailSender
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _config = config;

        //configure client
        //prepare email
        //send email
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var apiKey = _config["Mailgun:ApiKey"];
            var requestUrl = _config["Mailgun:BaseUrl"];
            var toEmail = _config["Mailgun:ToEmail"];

            // Validate that the required configuration values are present
            if (requestUrl == null || apiKey == null || toEmail == null)
            {
                ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<EmailSenderService>();
                throw new InvalidOperationException("Mailgun configuration is missing.");
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

            //receive the response and post to console if an error occured, move to proper logging later
            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Mailgun Error: {response.StatusCode} - {responseBody}");
                throw new InvalidOperationException($"Failed to send email: {responseBody}");
            }

            Console.WriteLine($"Mailgun Success: {responseBody}");
        }

    }
}
