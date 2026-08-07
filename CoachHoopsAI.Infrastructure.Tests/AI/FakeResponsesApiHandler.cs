using System.Net;
using System.Text;

namespace CoachHoopsAI.Infrastructure.Tests.AI;

// Stands in for the real OpenAI Responses API endpoint so tests never touch the
// network. Always returns the same canned body regardless of the request - these
// tests are only about what OpenAiSuggestionClientHttp does with a given response,
// not about how it builds the request.
internal sealed class FakeResponsesApiHandler(string responseBody) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
