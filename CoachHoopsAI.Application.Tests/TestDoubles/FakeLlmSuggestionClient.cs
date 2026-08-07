using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Application.Tests.TestDoubles;

// Stands in for the real OpenAI-backed client so orchestration tests never make
// network calls. Records what GameAnalysisService grounded it with.
public class FakeLlmSuggestionClient : ILlmSuggestionClient
{
    public IReadOnlyCollection<Suggestion> SuggestionsToReturn { get; set; } = Array.Empty<Suggestion>();

    public GameAnalysisInput? LastInput { get; private set; }
    public IReadOnlyCollection<ProblemTag>? LastTags { get; private set; }
    public GameDiagnostics? LastDiagnostics { get; private set; }
    public string? LastAppliedRulesProfile { get; private set; }
    public int CallCount { get; private set; }

    public Task<IReadOnlyCollection<Suggestion>> GetSuggestionsAsync(
        GameAnalysisInput input,
        IReadOnlyCollection<ProblemTag> tags,
        GameDiagnostics diagnostics,
        string appliedRulesProfile)
    {
        CallCount++;
        LastInput = input;
        LastTags = tags;
        LastDiagnostics = diagnostics;
        LastAppliedRulesProfile = appliedRulesProfile;
        return Task.FromResult(SuggestionsToReturn);
    }
}
