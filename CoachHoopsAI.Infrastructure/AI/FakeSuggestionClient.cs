using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;

namespace CoachHoopsAI.Infrastructure.AI
{
    public class FakeSuggestionClient : ILlmSuggestionClient
    {
        public Task<IReadOnlyCollection<Suggestion>> GetSuggestionsAsync(GameAnalysisInput input, IReadOnlyCollection<Domain.Enums.ProblemTag> tags)
        {
            return Task.FromResult<IReadOnlyCollection<Suggestion>>(Array.Empty<Suggestion>());
        }
    }
}
