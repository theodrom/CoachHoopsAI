using System.Text.Json;

namespace CoachHoopsAI.Infrastructure.Tests.AI;

// Builds a minimal OpenAI Responses API envelope containing exactly the
// output[].content[].{type:"output_text", text:"<structured-suggestions-json>"}
// shape OpenAiSuggestionClientHttp.ExtractFirstOutputText looks for, so tests can
// control what "the model returned" without depending on the real API.
internal static class ResponsesApiEnvelope
{
    public static string Build(params (string Category, string Text, string Reason)[] suggestions)
    {
        var suggestionsPayload = new
        {
            suggestions = suggestions
                .Select(s => new { category = s.Category, text = s.Text, reason = s.Reason })
                .ToArray()
        };
        var suggestionsJson = JsonSerializer.Serialize(suggestionsPayload);

        var envelope = new
        {
            output = new object[]
            {
                new
                {
                    content = new object[]
                    {
                        new { type = "output_text", text = suggestionsJson }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(envelope);
    }
}
