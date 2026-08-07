namespace CoachHoopsAI.Api.Contracts
{
    // Raw box-score counts. Mirrors CoachHoopsAI.Domain.Entities.TeamStats
    // property-for-property; AnalyzeGameMappings.ToGameAnalysisInput relies on that
    // to map every property by name (see AnalyzeGameMappingsTests).
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
