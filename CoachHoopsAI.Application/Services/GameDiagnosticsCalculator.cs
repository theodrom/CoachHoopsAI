using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;

namespace CoachHoopsAI.Application.Services
{
    public static class GameDiagnosticsCalculator
    {
        public static GameDiagnostics Calculate(TeamStats team, TeamStats opponent, string appliedRulesProfile)
        {
            return new GameDiagnostics(
                pointsDiff: team.Points - opponent.Points,
                turnoversDiff: team.Turnovers - opponent.Turnovers,
                offensiveReboundsDiff: team.OffensiveRebounds - opponent.OffensiveRebounds,
                defensiveReboundsDiff: team.DefensiveRebounds - opponent.DefensiveRebounds,

                teamFieldGoalPercentage: team.FieldGoalPercentage,
                opponentFieldGoalPercentage: opponent.FieldGoalPercentage,
                fieldGoalPctDiff: team.FieldGoalPercentage - opponent.FieldGoalPercentage,

                threePointPctDiff: team.ThreePointPercentage - opponent.ThreePointPercentage,
                threePointAttemptsDiff: team.ThreePointAttempts - opponent.ThreePointAttempts,
                foulsDiff: team.Fouls - opponent.Fouls,

                appliedRulesProfile: appliedRulesProfile
            );
        }
    }
}
