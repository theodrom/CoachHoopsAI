using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachHoopsAI.Application.Models
{
    public class AnalysisRecord
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

        public string Level { get; init; } = "";
        public string? RequestedRulesProfile { get; init; }
        public string AppliedRulesProfile { get; init; } = "";

        // Query helpers (duplicated fields for easy filtering)
        public DateTime? GameDate { get; init; }
        public string? TeamName { get; init; }
        public string? OpponentName { get; init; }
        public string? Season { get; init; }
        public string? Location { get; init; }

        // Versioning
        public string RulesetVersion { get; init; } = "1.2";
        public string PromptVersion { get; init; } = "v2.0";
        public string AiModel { get; init; } = "";

        // Snapshots (JSON TEXT)
        public string InputJson { get; init; } = "";
        public string ProblemTagsJson { get; init; } = "";
        public string DiagnosticsJson { get; init; } = "";
        public string SuggestionsJson { get; init; } = "";
    }
}
