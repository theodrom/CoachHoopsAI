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
        // Bumped to 1.4: OffensiveEfficiencyProblem now reads OffensiveRating
        // (points per 100 estimated possessions) with no score-margin gate, instead
        // of raw FG% gated on losing by a margin - same ProblemTag, reused rather
        // than retired, since the old trigger was a narrow proxy for the same
        // concept this now measures directly (see Docs/03-domain-and-rules.md's
        // "Findings (Milestone 3)" section, which also covers 1.3's changes).
        private const string RulesetVersion = "1.4";
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
