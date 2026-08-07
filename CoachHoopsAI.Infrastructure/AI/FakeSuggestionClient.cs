using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;

namespace CoachHoopsAI.Infrastructure.AI
{
    public class FakeSuggestionClient : ILlmSuggestionClient
    {
        public string ModelName => "Fake";

        public Task<IReadOnlyCollection<Suggestion>> GetSuggestionsAsync(
            GameAnalysisInput input,
            IReadOnlyCollection<Domain.Enums.ProblemTag> tags,
            GameDiagnostics diagnostics,
            string appliedRulesProfile)
        {
            return Task.FromResult<IReadOnlyCollection<Suggestion>>(Array.Empty<Suggestion>());
        }
    }
}
