using System.ComponentModel.DataAnnotations;

namespace CoachHoopsAI.Admin.Models
{
    public class TeamStatsDto
    {
        [Required]
        public int? Points { get; set; }
        [Range(0, 1)]
        public double? FieldGoalPercentage { get; set; }
        [Range(0, 1)]
        public double? ThreePointPercentage { get; set; }
        [Required]
        public int? ThreePointAttempts { get; set; }
        [Required]
        public int? OffensiveRebounds { get; set; }
        [Required]
        public int? DefensiveRebounds { get; set; }
        [Required]
        public int? Turnovers { get; set; }
        [Required]
        public int? Fouls { get; set; }
    }
}