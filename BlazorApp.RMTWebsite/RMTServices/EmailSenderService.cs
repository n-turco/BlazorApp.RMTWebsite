using BlazorApp.RMTWebsite.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace BlazorApp.RMTWebsite.RMTServices
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

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            if(toEmail != null) 
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "from", email },
                { "to", toEmail },
                { "subject", subject },
                { "text", htmlMessage }
            });
                request.Content = content;
            }

            await _httpClient.SendAsync(request);

         
        }
    }
}
