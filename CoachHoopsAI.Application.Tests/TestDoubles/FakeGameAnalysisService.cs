using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;

namespace CoachHoopsAI.Application.Tests.TestDoubles;

// Stands in for the real GameAnalysisService so AnalysisHistoryService tests can
// control exactly what result comes back (including AiModel) without depending
// on the rules engine, profile provider, or LLM client.
public class FakeGameAnalysisService : IGameAnalysisService
{
    public GameAnalysisResult ResultToReturn { get; set; } = new GameAnalysisResult();

    public GameAnalysisInput? LastInput { get; private set; }
    public int CallCount { get; private set; }

    public Task<GameAnalysisResult> AnalyzeAsync(GameAnalysisInput input)
    {
        CallCount++;
        LastInput = input;
        return Task.FromResult(ResultToReturn);
    }
}
