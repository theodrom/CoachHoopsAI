using System.Net;
using System.Text;

namespace CoachHoopsAI.Infrastructure.Tests.AI;

// Stands in for the real OpenAI Responses API endpoint so tests never touch the
// network. Always returns the same canned body regardless of the request - these
// tests are mostly about what OpenAiSuggestionClientHttp does with a given
// response, not about how it builds the request. LastRequestBody is an opt-in
// capture (unused by most tests) for the handful of tests that do need to inspect
// what was actually sent to the model.
internal sealed class FakeResponsesApiHandler(string responseBody) : HttpMessageHandler
{
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
        return response;
    }
}
