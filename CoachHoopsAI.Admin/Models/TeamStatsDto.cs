namespace CoachHoopsAI.Admin.Models
{
    // Raw box-score counts, mirroring CoachHoopsAI.Api.Contracts.TeamStatsDto.
    // Zero is a legitimate value for every field (e.g. early in a live game), so
    // these are plain non-nullable ints rather than [Required] nullable ints.
    public class TeamStatsDto
    {
        public int Points { get; set; }

        public int FieldGoalsMade { get; set; }
        public int FieldGoalsAttempted { get; set; }

        public int ThreePointsMade { get; set; }
        public int ThreePointsAttempted { get; set; }

        public int FreeThrowsMade { get; set; }
        public int FreeThrowsAttempted { get; set; }

        public int OffensiveRebounds { get; set; }
        public int DefensiveRebounds { get; set; }

        public int Assists { get; set; }
        public int Turnovers { get; set; }

        public int Steals { get; set; }
        public int Blocks { get; set; }

        public int PersonalFouls { get; set; }
    }
}
