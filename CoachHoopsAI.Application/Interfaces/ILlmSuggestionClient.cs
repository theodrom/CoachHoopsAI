using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;


namespace CoachHoopsAI.Application.Interfaces
{
    public interface ILlmSuggestionClient
    {
        Task<IReadOnlyCollection<Suggestion>> GetSuggestionsAsync(
            GameAnalysisInput input,
            IReadOnlyCollection<ProblemTag> tags);
    }
}
