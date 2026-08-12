namespace CoachHoopsAI.Domain.Metrics
{
    // Milestone 2B: the M2A single-team model, once per side, produced from a
    // Team/Opponent TeamStats pair by GameCalculatedMetricsCalculator. Team and
    // Opponent are calculated symmetrically - neither side is analytically
    // privileged (see Docs/03-domain-and-rules.md).
    //
    // Milestone 2C adds the two game-level (not per-side) tempo values below.
    // Both are symmetric under swapping Team/Opponent inputs.
    public sealed record GameCalculatedMetrics
    {
        public TeamCalculatedMetrics Team { get; init; } = new();
        public TeamCalculatedMetrics Opponent { get; init; } = new();

        // Mean of Team.EstimatedPossessions and Opponent.EstimatedPossessions - a
        // single game-level possession estimate, not a replacement for either side's
        // own value.
        public double GameEstimatedPossessions { get; init; }

        // Null when the calculator was not given GameFormat/GameTiming, or when
        // elapsed game time is zero (tip-off) - see GameCalculatedMetricsCalculator.
        public double? EstimatedPace { get; init; }
    }
}
