using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoachHoopsAI.Application.Services
{
    public sealed class AnalysisHistoryService : IAnalysisHistoryService
    {
        private readonly IGameAnalysisService _analysisService;
        private readonly IAnalysisRepository _repo;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // For V3.0 keep these constants in one place; later make them configurable.
        // Bumped to 1.6: DefensiveReboundProblem's trigger (an offensive-rebound
        // differential that never read team.DefensiveRebounds) is retired -
        // StatRulesEngine no longer emits it. LowDefensiveReboundPercentage
        // (appended, new ordinal) replaces it, reading DefensiveReboundPercentage
        // (M2B) directly - same ProblemTag.DefensiveReboundProblem kept only for
        // already-persisted history (see Docs/03-domain-and-rules.md's "Findings
        // (Milestone 3)" section, which also covers 1.3/1.4/1.5's changes).
        // Bumped to 1.7: PerimeterDefenseProblem is retired with no replacement tag
        // - its trigger was a causal ("perimeter defense") restatement of the same
        // opponent three-point evidence OpponentHotFromThree already measures
        // literally, gated more weakly and not tunable per level. Reviewed the same
        // way as 1.6's rebounding change, opposite conclusion from the reused
        // OffensiveEfficiencyProblem/TooManyThreePointAttempts triggers: this data
        // had no defensible connection to a defensive-positioning cause, so nothing
        // replaces it rather than reusing or duplicating the tag.
        private const string RulesetVersion = "1.7";
        private const string PromptVersion = "v2.0";

        public AnalysisHistoryService(IGameAnalysisService analysisService, IAnalysisRepository repo)
        {
            _analysisService = analysisService;
            _repo = repo;
        }

        public async Task<(GameAnalysisResult? Result, Guid AnalysisId)> AnalyzeAndStoreAsync(GameAnalysisInput input)
        {
            var result = await _analysisService.AnalyzeAsync(input);

            // Build input snapshot object (use your own stable structure)
            var inputSnapshot = new
            {
                input.Level,
                input.RulesProfile,
                input.Metadata,
                input.Notes,
                Team = input.Team,
                Opponent = input.Opponent
            };

            var record = new AnalysisRecord
            {
                Id = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow,

                Level = input.Level.ToString(),
                RequestedRulesProfile = input.RulesProfile,
                AppliedRulesProfile = result.Diagnostics != null ? result.Diagnostics.AppliedRulesProfile : string.Empty,

                GameDate = input.Metadata?.GameDate,
                TeamName = input.Metadata?.TeamName,
                OpponentName = input.Metadata?.OpponentName,
                Season = input.Metadata?.Season,
                Location = input.Metadata?.Location,

                RulesetVersion = RulesetVersion,
                PromptVersion = PromptVersion,
                AiModel = result?.AiModel ?? "",

                InputJson = JsonSerializer.Serialize(inputSnapshot, JsonOpts),
                ProblemTagsJson = JsonSerializer.Serialize(result?.ProblemTags, JsonOpts),
                DiagnosticsJson = JsonSerializer.Serialize(result?.Diagnostics, JsonOpts),
                SuggestionsJson = JsonSerializer.Serialize(result?.Suggestions, JsonOpts)
            };

            var id = await _repo.SaveAsync(record);
            return (result, id);
        }
    }
}
