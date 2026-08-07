using System;
using System.Collections.Generic;

namespace CoachHoopsAI.Admin.Models
{
    // Mirrors CoachHoopsAI.Api.Contracts.AnalyzeGameResponse / SuggestionsResponse /
    // CoachHoopsAI.Api.Models.SuggestionItemDto / CoachHoopsAI.Api.Contracts.GameDiagnosticsDto.
    // Keep these shapes in sync with the API contracts by hand until Admin shares a contracts project.
    public class AnalyzeGameResponseDto
    {
        public Guid AnalysisId { get; set; }
        public List<string>? ProblemTags { get; set; }
        public SuggestionsResponseDto Suggestions { get; set; } = new SuggestionsResponseDto();
        public GameDiagnosticsDto Diagnostics { get; set; } = new GameDiagnosticsDto();
    }

    public class SuggestionsResponseDto
    {
        public IReadOnlyCollection<SuggestionItemDto>? Offense { get; set; }
        public IReadOnlyCollection<SuggestionItemDto>? Defense { get; set; }
        public IReadOnlyCollection<SuggestionItemDto>? Other { get; set; }
    }

    public class SuggestionItemDto
    {
        public string? Suggestion { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class GameDiagnosticsDto
    {
        public int PointsDiff { get; set; }
        public int TurnoversDiff { get; set; }

        public int OffensiveReboundsDiff { get; set; }
        public int DefensiveReboundsDiff { get; set; }

        public double ThreePointPctDiff { get; set; }
        public int ThreePointAttemptsDiff { get; set; }

        public int FoulsDiff { get; set; }

        public double TeamFieldGoalPercentage { get; set; }
        public double OpponentFieldGoalPercentage { get; set; }
        public double FieldGoalPctDiff { get; set; }

        public string AppliedRulesProfile { get; set; } = string.Empty;
    }
}
