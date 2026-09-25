using System.Net;

namespace BlazorApp.RMTWebsite.Tests.Fakes;

/// <summary>
/// Stands in for the network during tests. An <see cref="HttpClient"/> hands every request to its
/// message handler, so giving the client this handler means nothing ever leaves the machine.
/// It remembers the last request it received and replies with whatever status code the test chose.
/// </summary>
public class FakeHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, string responseBody = "{}")
    : HttpMessageHandler
{
    /// <summary>The last request the code under test tried to send, or null if it sent nothing.</summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>
    /// The request body as text. It is read here, while the request is still alive,
    /// because the content may be disposed once the caller is finished with it.
    /// </summary>
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Record what was sent so the test can make assertions about it afterwards.
        LastRequest = request;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        // Reply with the canned response instead of calling Mailgun.
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody)
        };
    }
}
