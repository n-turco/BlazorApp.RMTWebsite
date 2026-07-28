using BlazorApp.RMTWebsite.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


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
        //public static async Task<RestResponse> Send()
        //{
        //    var options = new RestClientOptions("https://api.mailgun.net")
        //    {
        //        Authenticator = new HttpBasicAuthenticator("api", Environment.GetEnvironmentVariable("API_KEY") ?? "API_KEY")
        //    };

        //    var client = new RestClient(options);
        //    var request = new RestRequest("/v3/sandbox29ee27561c3e452ea58e840ba561dcb8.mailgun.org/messages", Method.Post);
        //    request.AlwaysMultipartFormData = true;
        //    request.AddParameter("from", "Mailgun Sandbox <postmaster@sandbox29ee27561c3e452ea58e840ba561dcb8.mailgun.org>");
        //    request.AddParameter("to", "Nicholas Turco <nicholas.turco@hotmail.com>");
        //    request.AddParameter("subject", "Hello Nicholas Turco");
        //    request.AddParameter("text", "Congratulations Nicholas Turco, you just sent an email with Mailgun! You are truly awesome!");
        //    return await client.ExecuteAsync(request);
        //}

    }
}
