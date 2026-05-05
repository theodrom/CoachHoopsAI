using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Rules;

namespace CoachHoopsAI.Application.Services
{
    // GameAnalysisService responsibilities:
    // - Accept GameAnalysisInput(stats + notes + level).
    // - Call Domain rules engine → get ProblemTag[].
    // - Call AI client(ILlmSuggestionClient) with:
    //      - Stats
    //      - Tags
    //      - Notes
    // Return structured GameAnalysisResult.
    public class GameAnalysisService : IGameAnalysisService
    {
        private readonly IStatRulesEngine _rulesEngine;
        private readonly ILlmSuggestionClient _llmClient;
        private readonly IRulesProfileProvider _profileProvider;

        public GameAnalysisService(
            IStatRulesEngine rulesEngine,
            ILlmSuggestionClient llmClient,
            IRulesProfileProvider profileProvider)
        {
            _rulesEngine = rulesEngine;
            _llmClient = llmClient;
            _profileProvider = profileProvider;
        }

        public async Task<GameAnalysisResult> AnalyzeAsync(GameAnalysisInput input)
        {
            var resolved = _profileProvider.Resolve(input.Level, input.RulesProfile);

            var tags = _rulesEngine.Evaluate(input.Team, input.Opponent, resolved.Profile);

            // Diagnostics are computed before the AI call so they can be used as
            // primary grounding signals in the suggestion prompt.
            var diagnostics = GameDiagnosticsCalculator.Calculate(input.Team, input.Opponent, resolved.Name);

            var suggestions = await _llmClient.GetSuggestionsAsync(input, tags, diagnostics, resolved.Name);

            return new GameAnalysisResult
            {
                Level = input.Level,
                ProblemTags = tags,
                Suggestions = suggestions,
                Diagnostics = diagnostics
            };
        }
    }
}
