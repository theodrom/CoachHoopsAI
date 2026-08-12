namespace CoachHoopsAI.Domain.Metrics
{
    // Numerical facts derived from a single TeamStats instance - no thresholds, no
    // judgments, no opponent/game-context dependence. Ratios are normalized decimals
    // (e.g. 0.425, not 42.5) and are not rounded here; presentation owns formatting.
    // See Docs/03-domain-and-rules.md and CalculatedMetricsCalculator.
    public sealed record TeamCalculatedMetrics
    {
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
    }
}
