using CoachHoopsAI.Infrastructure.AI;

namespace CoachHoopsAI.Infrastructure.Tests.AI;

// Confirms OpenAiSuggestionClientHttp.ModelName reports the single configured
// OpenAiOptions.Model value - the model identifier that AnalysisHistoryService
// ultimately persists as AnalysisRecord.AiModel. No HTTP call is made; ModelName
// is a plain property read.
public class OpenAiSuggestionClientModelNameTests
{
    [Fact]
    public void ModelName_ReturnsConfiguredModel()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new OpenAiOptions
        {
            ApiKey = "test-key",
            Model = "gpt-4.1-mini",
            BaseUrl = "http://localhost"
        });
        var client = new OpenAiSuggestionClientHttp(options, new HttpClient());

        Assert.Equal("gpt-4.1-mini", client.ModelName);
    }
}
