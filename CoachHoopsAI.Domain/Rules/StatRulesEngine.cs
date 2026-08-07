using CoachHoopsAI.Domain.Compatibility;
using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Domain.Rules
{
    public class StatRulesEngine : IStatRulesEngine
    {
        public IReadOnlyCollection<ProblemTag> Evaluate(TeamStats team, TeamStats opponent, RulesProfile profile)
        {
            profile ??= new RulesProfile();

            var tags = new List<ProblemTag>();

            var teamFieldGoalPct = LegacyPercentageBridge.FieldGoalPercentage(team);
            var teamThreePointPct = LegacyPercentageBridge.ThreePointPercentage(team);
            var opponentFieldGoalPct = LegacyPercentageBridge.FieldGoalPercentage(opponent);
            var opponentThreePointPct = LegacyPercentageBridge.ThreePointPercentage(opponent);

            // Offense
            if ((team.Turnovers - opponent.Turnovers) >= profile.TurnoverDiffToFlag)
                tags.Add(ProblemTag.TurnoverProblem);

            if (teamThreePointPct <= profile.OurBadThreePct && team.ThreePointsAttempted >= profile.OurBadThreeAttemptsMin)
                tags.Add(ProblemTag.OurShootingInefficiency);

            if (team.ThreePointsAttempted >= profile.TooManyThreeAttemptsMin && teamThreePointPct <= profile.TooManyThreePctMax)
                tags.Add(ProblemTag.TooManyThreePointAttempts);

            if ((opponent.Points - team.Points) >= profile.LossByPointsToFlagOffensiveEfficiency && teamFieldGoalPct <= profile.OurLowFieldGoalPctForOffensiveEfficiency)
                tags.Add(ProblemTag.OffensiveEfficiencyProblem);

            if (team.PersonalFouls <= opponent.PersonalFouls - 5 && team.Points <= opponent.Points)
                tags.Add(ProblemTag.LackOfPaintPressure);


            // Defense
            if (opponentFieldGoalPct >= profile.OpponentHighFieldGoalPct)
                tags.Add(ProblemTag.InteriorDefenseProblem);

            if (opponentThreePointPct >= profile.OpponentHotThreePct && opponent.ThreePointsAttempted >= profile.OpponentHotThreeAttemptsMin)
                tags.Add(ProblemTag.OpponentHotFromThree);

            if (opponentThreePointPct >= 0.36 && opponent.ThreePointsAttempted >= team.ThreePointsAttempted + 5)
                tags.Add(ProblemTag.PerimeterDefenseProblem);

            if ((opponent.Points - team.Points) >= profile.LossByPointsToFlagTransition && team.Turnovers >= profile.TurnoversMinToFlagTransition)
                tags.Add(ProblemTag.TransitionDefenseProblem);

            if (team.PersonalFouls - opponent.PersonalFouls >= profile.FoulsDiffToFlag)
                tags.Add(ProblemTag.FoulsProblem);


            // Rebounding
            if (opponent.OffensiveRebounds - team.OffensiveRebounds >= profile.OpponentOffensiveReboundDiffToFlag)
                tags.Add(ProblemTag.DefensiveReboundProblem);


            // Game Control
            if (Math.Abs(team.Points - opponent.Points) >= 15)
                tags.Add(ProblemTag.PaceControlProblem);


            return tags;
        }
    }
}
