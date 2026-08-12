using CoachHoopsAI.Domain.Entities;

namespace CoachHoopsAI.Domain.Metrics
{
    // Milestone 2B: possession-dependent and opponent-dependent metrics for a
    // Team/Opponent TeamStats pair. Reuses CalculatedMetricsCalculator (M2A) for each
    // side's single-team metrics rather than re-deriving them, then enriches both
    // sides symmetrically - swapping the two arguments swaps Team/Opponent in the
    // result. Current default formulas only; not yet consumed by rules, diagnostics,
    // the LLM prompt, Admin, persistence, or API responses (see CLAUDE.md M2B scope).
    public static class GameCalculatedMetricsCalculator
    {
        public static GameCalculatedMetrics Calculate(TeamStats team, TeamStats opponent)
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

            return new GameCalculatedMetrics { Team = teamMetrics, Opponent = opponentMetrics };
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
