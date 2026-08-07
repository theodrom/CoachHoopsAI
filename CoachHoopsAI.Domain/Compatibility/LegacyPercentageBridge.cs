using CoachHoopsAI.Domain.Entities;

namespace CoachHoopsAI.Domain.Compatibility
{
    // Milestone 1 scaffolding only. StatRulesEngine's thresholds were written against
    // shooting percentages, but TeamStats now stores only raw made/attempted counts.
    // This computes exactly the two ratios the current rules need, on the fly, so the
    // existing rule thresholds keep behaving the same way without introducing the
    // full calculated-metrics layer (eFG%, possessions, rates, etc.) planned for
    // Milestone 2. Delete this class once the rules engine is redesigned to read raw
    // counts directly.
    public static class LegacyPercentageBridge
    {
        public static double FieldGoalPercentage(TeamStats stats) =>
            stats.FieldGoalsAttempted == 0 ? 0.0 : (double)stats.FieldGoalsMade / stats.FieldGoalsAttempted;

        public static double ThreePointPercentage(TeamStats stats) =>
            stats.ThreePointsAttempted == 0 ? 0.0 : (double)stats.ThreePointsMade / stats.ThreePointsAttempted;
    }
}
