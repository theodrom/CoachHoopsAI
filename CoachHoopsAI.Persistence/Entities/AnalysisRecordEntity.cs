using System;

namespace CoachHoopsAI.Persistence.Entities
{
    public sealed class AnalysisRecordEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedUtc { get; set; }

        public string Level { get; set; } = "";
        public string? RequestedRulesProfile { get; set; }
        public string AppliedRulesProfile { get; set; } = "";


        public DateTime? GameDate { get; set; }
        public string? TeamName { get; set; }
        public string? OpponentName { get; set; }
        public string? Season { get; set; }
        public string? Location { get; set; }

        public string RulesetVersion { get; set; } = "1.2";
        public string PromptVersion { get; set; } = "v2.0";
        public string AiModel { get; set; } = "";

        public string InputJson { get; set; } = "";
        public string ProblemTagsJson { get; set; } = "";
        public string DiagnosticsJson { get; set; } = "";
        public string SuggestionsJson { get; set; } = "";
    }
}
