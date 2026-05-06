using System;
using System.ComponentModel.DataAnnotations;

namespace CoachHoopsAI.Admin.Models
{
    public class AnalyzeGameRequestDto
    {
        public TeamStatsDto Team { get; set; } = new();
        public TeamStatsDto Opponent { get; set; } = new();

        [Required]
        public string? Level { get; set; }

        public string? Notes { get; set; }
        public DateTime? GameDate { get; set; }
        public string? TeamName { get; set; }
        public string? OpponentName { get; set; }
        public string? Competition { get; set; }
        public string? Season { get; set; }
        public string? Location { get; set; }
        public string? RulesProfile { get; set; }
    }
}