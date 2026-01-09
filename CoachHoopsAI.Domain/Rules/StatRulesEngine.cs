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


            // Offense
            if ((team.Turnovers - opponent.Turnovers) >= profile.TurnoverDiffToFlag)
                tags.Add(ProblemTag.TurnoverProblem);

            if (team.ThreePointPercentage <= profile.OurBadThreePct && team.ThreePointAttempts >= profile.OurBadThreeAttemptsMin)
                tags.Add(ProblemTag.OurShootingInefficiency);

            if (team.ThreePointAttempts >= profile.TooManyThreeAttemptsMin && team.ThreePointPercentage <= profile.TooManyThreePctMax)
                tags.Add(ProblemTag.TooManyThreePointAttempts);

            if ((opponent.Points - team.Points) >= profile.LossByPointsToFlagOffensiveEfficiency && team.FieldGoalPercentage <= profile.OurLowFieldGoalPctForOffensiveEfficiency)
                tags.Add(ProblemTag.OffensiveEfficiencyProblem);

            if (team.Fouls <= opponent.Fouls - 5 && team.Points <= opponent.Points)
                tags.Add(ProblemTag.LackOfPaintPressure);


            // Defense
            if (opponent.FieldGoalPercentage >= profile.OpponentHighFieldGoalPct)
                tags.Add(ProblemTag.InteriorDefenseProblem);

            if (opponent.ThreePointPercentage >= profile.OpponentHotThreePct && opponent.ThreePointAttempts >= profile.OpponentHotThreeAttemptsMin)
                tags.Add(ProblemTag.OpponentHotFromThree);

            if (opponent.ThreePointPercentage >= 0.36 && opponent.ThreePointAttempts >= team.ThreePointAttempts + 5)
                tags.Add(ProblemTag.PerimeterDefenseProblem);

            if ((opponent.Points - team.Points) >= profile.LossByPointsToFlagTransition && team.Turnovers >= profile.TurnoversMinToFlagTransition)
                tags.Add(ProblemTag.TransitionDefenseProblem);

            if (team.Fouls - opponent.Fouls >= profile.FoulsDiffToFlag)
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
