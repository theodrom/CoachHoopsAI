using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;


namespace CoachHoopsAI.Application.Interfaces
{
    public interface ILlmSuggestionClient
    {
        // The identifier of the model this client is configured to use (e.g. an
        // OpenAI model name, or a descriptive label for a non-LLM implementation).
        // Provider-neutral: callers must not assume any particular format.
        string ModelName { get; }

        Task<IReadOnlyCollection<Suggestion>> GetSuggestionsAsync(
            GameAnalysisInput input,
            IReadOnlyCollection<ProblemTag> tags,
            GameDiagnostics diagnostics,
            string appliedRulesProfile);
    }
}
