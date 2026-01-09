namespace CoachHoopsAI.Api.Contracts
{
    public class GameDiagnosticsDto
    {
        public int PointsDiff { get; set; }                 // Team - Opponent
        public int TurnoversDiff { get; set; }              // Team - Opponent

        public int OffensiveReboundsDiff { get; set; }      // Team ORB - Opponent ORB
        public int DefensiveReboundsDiff { get; set; }      // Team DRB - Opponent DRB

        public double ThreePointPctDiff { get; set; }       // Team - Opponent
        public int ThreePointAttemptsDiff { get; set; }     // Team - Opponent

        public int FoulsDiff { get; set; }                  // Team - Opponent

        public double TeamFieldGoalPercentage { get; set; }
        public double OpponentFieldGoalPercentage { get; set; }
        public double FieldGoalPctDiff { get; set; } // Team - Opponent

        public string AppliedRulesProfile { get; set; } = string.Empty;

    }
}
