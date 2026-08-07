namespace CoachHoopsAI.Domain.Entities
{
    // Raw box-score observations for one team in one game. These are facts recorded
    // from the game itself - shooting percentages, rebound totals, and every other
    // derived basketball metric are calculated from these counts elsewhere, never
    // stored as an input here.
    public sealed record TeamStats
    {
        public int Points { get; init; }

        public int FieldGoalsMade { get; init; }
        public int FieldGoalsAttempted { get; init; }

        public int ThreePointsMade { get; init; }
        public int ThreePointsAttempted { get; init; }

        public int FreeThrowsMade { get; init; }
        public int FreeThrowsAttempted { get; init; }

        public int OffensiveRebounds { get; init; }
        public int DefensiveRebounds { get; init; }

        public int Assists { get; init; }
        public int Turnovers { get; init; }

        public int Steals { get; init; }
        public int Blocks { get; init; }

        public int PersonalFouls { get; init; }
    }
}
