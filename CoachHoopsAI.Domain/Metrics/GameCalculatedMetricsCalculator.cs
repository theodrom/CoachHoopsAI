using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.GameContext;

namespace CoachHoopsAI.Domain.Metrics
{
    // Milestone 2B: possession-dependent and opponent-dependent metrics for a
    // Team/Opponent TeamStats pair. Reuses CalculatedMetricsCalculator (M2A) for each
    // side's single-team metrics rather than re-deriving them, then enriches both
    // sides symmetrically - swapping the two arguments swaps Team/Opponent in the
    // result.
    //
    // Milestone 2C adds Estimated Pace, a game-level (not per-side) tempo metric
    // built on top of the M2B possession estimates plus GameFormat/GameTiming from
    // Milestone 1. The two-TeamStats overload keeps working exactly as before -
    // EstimatedPace is simply unavailable (null) without timing, never fabricated
    // from a dummy GameTiming. Current default formulas only; not yet consumed by
    // rules, diagnostics, the LLM prompt, Admin, persistence, or API responses (see
    // CLAUDE.md M2 scope).
    public static class GameCalculatedMetricsCalculator
    {
        public static GameCalculatedMetrics Calculate(TeamStats team, TeamStats opponent) =>
            CalculateCore(team, opponent);

        public static GameCalculatedMetrics Calculate(TeamStats team, TeamStats opponent, GameFormat format, GameTiming timing)
        {
            var core = CalculateCore(team, opponent);

            var elapsedMinutes = timing.ElapsedGameTime(format).TotalMinutes;
            var pace = elapsedMinutes == 0
                ? (double?)null
                : core.GameEstimatedPossessions * format.RegulationDuration.TotalMinutes / elapsedMinutes;

            return core with { EstimatedPace = pace };
        }

        private static GameCalculatedMetrics CalculateCore(TeamStats team, TeamStats opponent)
        {
            var teamPossessions = EstimatedPossessions(team);
            var opponentPossessions = EstimatedPossessions(opponent);

            var teamMetrics = CalculatedMetricsCalculator.Calculate(team) with
            {
                EstimatedPossessions = teamPossessions,
                OffensiveReboundPercentage = ReboundPercentage(team.OffensiveRebounds, opponent.DefensiveRebounds),
                DefensiveReboundPercentage = ReboundPercentage(team.DefensiveRebounds, opponent.OffensiveRebounds),
                TurnoverRate = RateOrNull(team.Turnovers, teamPossessions),
                StealRate = RateOrNull(team.Steals, opponentPossessions),
                FoulRate = RateOrNull(team.PersonalFouls, opponentPossessions),
                OffensiveRating = RateOrNull(100.0 * team.Points, teamPossessions),
            };

            var opponentMetrics = CalculatedMetricsCalculator.Calculate(opponent) with
            {
                EstimatedPossessions = opponentPossessions,
                OffensiveReboundPercentage = ReboundPercentage(opponent.OffensiveRebounds, team.DefensiveRebounds),
                DefensiveReboundPercentage = ReboundPercentage(opponent.DefensiveRebounds, team.OffensiveRebounds),
                TurnoverRate = RateOrNull(opponent.Turnovers, opponentPossessions),
                StealRate = RateOrNull(opponent.Steals, teamPossessions),
                FoulRate = RateOrNull(opponent.PersonalFouls, teamPossessions),
                OffensiveRating = RateOrNull(100.0 * opponent.Points, opponentPossessions),
            };

            return new GameCalculatedMetrics
            {
                Team = teamMetrics,
                Opponent = opponentMetrics,
                GameEstimatedPossessions = (teamPossessions + opponentPossessions) / 2.0,
            };
        }

        private static double EstimatedPossessions(TeamStats stats) =>
            stats.FieldGoalsAttempted - stats.OffensiveRebounds + stats.Turnovers + 0.44 * stats.FreeThrowsAttempted;

        private static double? ReboundPercentage(int side, int opponentOtherSide)
        {
            var denominator = side + opponentOtherSide;
            return denominator == 0 ? null : (double)side / denominator;
        }

        private static double? RateOrNull(double numerator, double denominator) =>
            denominator == 0 ? null : numerator / denominator;
    }
}
