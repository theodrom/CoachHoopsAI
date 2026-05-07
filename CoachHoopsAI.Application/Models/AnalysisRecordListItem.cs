using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachHoopsAI.Application.Models
{
    public class AnalysisRecordListItem
    {
        public Guid Id { get; init; }
        public DateTime CreatedUtc { get; init; }

        public string Level { get; init; } = "";
        public string AppliedRulesProfile { get; init; } = "";

        public DateTime? GameDate { get; init; }
        public string? TeamName { get; init; }
        public string? OpponentName { get; init; }
        public string? Season { get; init; }
        public string? Location { get; init; }
        public string? ProblemTagsJson { get; init; }
        public string? AiModel { get; init; }
    }
}
