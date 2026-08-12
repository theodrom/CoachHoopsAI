using CoachHoopsAI.Domain.Entities;

namespace CoachHoopsAI.Domain.Metrics
{
    // Milestone 2A: deterministic core metrics computed from a single TeamStats
    // instance. Depends on nothing else - no opponent stats, GameFormat, GameTiming,
    // configuration, or services. Current default formulas only; not a
    // configuration framework, and not yet consumed by rules, diagnostics, the LLM
    // prompt, Admin, persistence, or API responses (see CLAUDE.md M2A scope).
    public static class CalculatedMetricsCalculator
    {
        public static TeamCalculatedMetrics Calculate(TeamStats stats) => new()
        {
            FieldGoalPercentage = stats.FieldGoalsAttempted == 0
                ? 0.0
                : (double)stats.FieldGoalsMade / stats.FieldGoalsAttempted,

            ThreePointPercentage = stats.ThreePointsAttempted == 0
                ? 0.0
                : (double)stats.ThreePointsMade / stats.ThreePointsAttempted,

            FreeThrowPercentage = stats.FreeThrowsAttempted == 0
                ? 0.0
                : (double)stats.FreeThrowsMade / stats.FreeThrowsAttempted,

            TotalRebounds = stats.OffensiveRebounds + stats.DefensiveRebounds,

            EffectiveFieldGoalPercentage = stats.FieldGoalsAttempted == 0
                ? 0.0
                : (stats.FieldGoalsMade + 0.5 * stats.ThreePointsMade) / stats.FieldGoalsAttempted,

            AssistToTurnoverRatio = stats.Turnovers == 0
                ? null
                : (double)stats.Assists / stats.Turnovers,

            ThreePointAttemptRate = stats.FieldGoalsAttempted == 0
                ? 0.0
                : (double)stats.ThreePointsAttempted / stats.FieldGoalsAttempted,

            FreeThrowRate = stats.FieldGoalsAttempted == 0
                ? 0.0
                : (double)stats.FreeThrowsAttempted / stats.FieldGoalsAttempted,
        };
    }
}
