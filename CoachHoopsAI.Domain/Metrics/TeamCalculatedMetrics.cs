namespace CoachHoopsAI.Domain.Metrics
{
    // Numerical facts for one side (Team or Opponent) - no thresholds, no judgments.
    // Ratios are normalized decimals (e.g. 0.425, not 42.5) and are not rounded here;
    // presentation owns formatting. See Docs/03-domain-and-rules.md,
    // CalculatedMetricsCalculator (M2A, single-TeamStats fields below) and
    // GameCalculatedMetricsCalculator (M2B, possession/opponent-dependent fields below).
    public sealed record TeamCalculatedMetrics
    {
        // M2A - derived from this side's TeamStats alone.
        public double FieldGoalPercentage { get; init; }
        public double ThreePointPercentage { get; init; }
        public double FreeThrowPercentage { get; init; }
        public int TotalRebounds { get; init; }
        public double EffectiveFieldGoalPercentage { get; init; }

        // Null when Turnovers == 0 - a zero-turnover performance has no meaningful
        // ratio and must not be forced to zero.
        public double? AssistToTurnoverRatio { get; init; }

        public double ThreePointAttemptRate { get; init; }
        public double FreeThrowRate { get; init; }

        // M2B - derived from this side's TeamStats together with the opposing side's.
        public double EstimatedPossessions { get; init; }

        // Null when the relevant denominator is zero - see GameCalculatedMetricsCalculator.
        public double? OffensiveReboundPercentage { get; init; }
        public double? DefensiveReboundPercentage { get; init; }
        public double? TurnoverRate { get; init; }
        public double? StealRate { get; init; }
        public double? FoulRate { get; init; }
        public double? OffensiveRating { get; init; }
    }
}
