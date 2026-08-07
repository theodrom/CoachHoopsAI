using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Compatibility;
using CoachHoopsAI.Domain.Entities;

namespace CoachHoopsAI.Application.Services
{
    public static class GameDiagnosticsCalculator
    {
        public static GameDiagnostics Calculate(TeamStats team, TeamStats opponent, string appliedRulesProfile)
        {
            var teamFieldGoalPct = LegacyPercentageBridge.FieldGoalPercentage(team);
            var opponentFieldGoalPct = LegacyPercentageBridge.FieldGoalPercentage(opponent);
            var teamThreePointPct = LegacyPercentageBridge.ThreePointPercentage(team);
            var opponentThreePointPct = LegacyPercentageBridge.ThreePointPercentage(opponent);

            return new GameDiagnostics(
                pointsDiff: team.Points - opponent.Points,
                turnoversDiff: team.Turnovers - opponent.Turnovers,
                offensiveReboundsDiff: team.OffensiveRebounds - opponent.OffensiveRebounds,
                defensiveReboundsDiff: team.DefensiveRebounds - opponent.DefensiveRebounds,

                teamFieldGoalPercentage: teamFieldGoalPct,
                opponentFieldGoalPercentage: opponentFieldGoalPct,
                fieldGoalPctDiff: teamFieldGoalPct - opponentFieldGoalPct,

                threePointPctDiff: teamThreePointPct - opponentThreePointPct,
                threePointAttemptsDiff: team.ThreePointsAttempted - opponent.ThreePointsAttempted,
                foulsDiff: team.PersonalFouls - opponent.PersonalFouls,

                appliedRulesProfile: appliedRulesProfile
            );
        }
    }
}
