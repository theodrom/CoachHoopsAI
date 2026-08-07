
namespace CoachHoopsAI.Api.Contracts
{
    public class AnalyzeGameRequest
    {
        public TeamStatsDto? Team { get; set; }
        public TeamStatsDto? Opponent { get; set; }

        /// <summary>
        /// Youth, Amateur, Pro
        /// </summary>
        public string? Level { get; set; }

        /// <summary>
        /// Optional. Coach notes about the match: 
        /// e.g. "We struggled with their PnR" or "Our big was in foul trouble."
        /// </summary>
        public string? Notes { get; set; }

        // V1.1 optional metadata
        public DateTime? GameDate { get; set; }          // ISO 8601 in JSON
        public string? TeamName { get; set; }
        public string? OpponentName { get; set; }
        public string? Competition { get; set; }         // e.g. "U14 League", "Division 2"
        public string? Season { get; set; }              // e.g. "2025/2026"
        public string? Location { get; set; }            // optional: gym/arena/city

        // V1.3: optional rules profile override
        // If null/empty -> use default profile for the given Level
        public string? RulesProfile { get; set; } = null;

        // V1.4 (Milestone 1): the structure of this specific game and where it
        // currently stands. Independent of Level - Level selects the analysis/rules
        // profile (EasyBasket/Youth/Amateur/Pro), GameFormat describes the actual
        // game being played (e.g. a Youth game can use a 4x10 or a 4x5 format).
        public GameFormatDto? GameFormat { get; set; }
        public GameTimingDto? GameTiming { get; set; }
    }
}
