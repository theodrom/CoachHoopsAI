namespace CoachHoopsAI.Api.Contracts
{
    public class TeamStatsDto
    {
        public int Points { get; set; }
        public double FieldGoalPercentage { get; set; }
        public double ThreePointPercentage { get; set; }
        public int ThreePointAttempts { get; set; }
        public int OffensiveRebounds { get; set; }
        public int DefensiveRebounds { get; set; }
        public int Turnovers { get; set; }
        public int Fouls { get; set; }
    }
}
