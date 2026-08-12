namespace CoachHoopsAI.Domain.Metrics
{
    // Milestone 2B: the M2A single-team model, once per side, produced from a
    // Team/Opponent TeamStats pair by GameCalculatedMetricsCalculator. Team and
    // Opponent are calculated symmetrically - neither side is analytically
    // privileged (see Docs/03-domain-and-rules.md).
    public sealed record GameCalculatedMetrics
    {
        public TeamCalculatedMetrics Team { get; init; } = new();
        public TeamCalculatedMetrics Opponent { get; init; } = new();
    }
}
