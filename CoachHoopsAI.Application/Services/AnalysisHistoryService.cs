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
        // Bumped to 1.8: InteriorDefenseProblem is retired - its trigger (opponent's
        // raw, unweighted FG% with no minimum-attempts gate at all) had no
        // defensible connection to "interior" defense specifically and no sample
        // protection whatsoever. Unlike 1.7's PerimeterDefenseProblem, this signal
        // (opponent overall shooting efficiency) was not already covered elsewhere,
        // so it is replaced - not just retired - by the appended
        // HighOpponentEffectiveFieldGoalPercentage, reading the opponent's
        // EffectiveFieldGoalPercentage (M2A via M2B) with a real FieldGoalsAttempted
        // minimum, distinct from OpponentHotFromThree's three-point-specific signal.
        // Bumped to 1.9: TransitionDefenseProblem is retired with no replacement tag
        // - its trigger (score margin AND an absolute team-turnover count) read no
        // fast-break, points-off-turnovers, or possession-sequencing data connecting
        // a turnover to the opponent scoring off it, and the only measurable fact it
        // used (elevated team turnovers) is already covered by TurnoverProblem. Same
        // pattern as 1.7's PerimeterDefenseProblem: the measurable part is redundant
        // with an existing finding, so nothing replaces it.
        // Bumped to 1.10: FoulsProblem's differential-only trigger (unchanged, still
        // fires exactly as before) was found under-inclusive - it could hide a real
        // foul problem when both teams foul heavily (e.g. 20 fouls to 17, diff 3,
        // never fired). Added FoulsHighCountToFlag, an absolute foul-count condition
        // OR'd with the existing differential, so previously-firing inputs still
        // fire and a new class of high-total-on-both-sides games now also fires.
        // Deliberately stayed in plain foul-count terms rather than migrating to
        // FoulRate (M2B) - see Docs/03-domain-and-rules.md.
        // Bumped to 1.11: TurnoverProblem's differential-only trigger (unchanged,
        // still fires exactly as before) was found under-inclusive for the same
        // reason as 1.10's FoulsProblem change - it could hide a real turnover
        // problem when both teams turn it over heavily. Added
        // TurnoverHighCountToFlag, an absolute turnover-count condition OR'd with
        // the existing differential, so previously-firing inputs still fire and a
        // new class of high-total-on-both-sides games now also fires. Deliberately
        // stayed in plain turnover-count terms rather than migrating to
        // TurnoverRate (M2B) - a deliberate product decision, not an oversight -
        // see Docs/03-domain-and-rules.md.
        private const string RulesetVersion = "1.11";
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
