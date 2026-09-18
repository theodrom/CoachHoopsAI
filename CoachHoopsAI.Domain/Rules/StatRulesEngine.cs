using CoachHoopsAI.Domain.Compatibility;
using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using CoachHoopsAI.Domain.Metrics;
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

            // Milestone 3's rules that read the Milestone 2 calculated-metrics layer
            // directly, instead of LegacyPercentageBridge.
            var teamMetrics = CalculatedMetricsCalculator.Calculate(team);

            // Offense
            if ((team.Turnovers - opponent.Turnovers) >= profile.TurnoverDiffToFlag)
                tags.Add(ProblemTag.TurnoverProblem);

            if (teamThreePointPct <= profile.OurBadThreePct && team.ThreePointsAttempted >= profile.OurBadThreeAttemptsMin)
                tags.Add(ProblemTag.OurShootingInefficiency);

            // Minimum-attempts gate is required, not optional: CalculatedMetricsCalculator
            // returns EffectiveFieldGoalPercentage == 0.0 when FieldGoalsAttempted == 0
            // (Milestone 2A's zero-denominator convention), which would otherwise read as
            // "maximally inefficient" for a team that simply hasn't shot yet.
            if (team.FieldGoalsAttempted >= profile.OurLowEffectiveFieldGoalPctAttemptsMin
                && teamMetrics.EffectiveFieldGoalPercentage <= profile.OurLowEffectiveFieldGoalPct)
                tags.Add(ProblemTag.LowEffectiveFieldGoalPercentage);

            // Replaces the old LackOfPaintPressure trigger below (personal fouls +
            // score), which had no defensible connection to what it claimed to
            // measure. FreeThrowRate == 0.0 when FieldGoalsAttempted == 0 (same M2A
            // convention as above), so the same kind of attempts gate applies here.
            if (team.FieldGoalsAttempted >= profile.OurLowFreeThrowRateAttemptsMin
                && teamMetrics.FreeThrowRate <= profile.OurLowFreeThrowRate)
                tags.Add(ProblemTag.LowFreeThrowRate);

            if (team.ThreePointsAttempted >= profile.TooManyThreeAttemptsMin && teamThreePointPct <= profile.TooManyThreePctMax)
                tags.Add(ProblemTag.TooManyThreePointAttempts);

            if ((opponent.Points - team.Points) >= profile.LossByPointsToFlagOffensiveEfficiency && teamFieldGoalPct <= profile.OurLowFieldGoalPctForOffensiveEfficiency)
                tags.Add(ProblemTag.OffensiveEfficiencyProblem);

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
