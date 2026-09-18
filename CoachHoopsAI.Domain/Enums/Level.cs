
namespace CoachHoopsAI.Domain.Enums
{
    public enum Level
    {
        EasyBasket,
        Youth,
        Amateur,
        Pro
    }

    public enum ProblemTag
    {
        None = 0,

        // Offense
        TurnoverProblem,
        OffensiveEfficiencyProblem,
        OurShootingInefficiency,
        TooManyThreePointAttempts,
        LackOfPaintPressure,

        // Rebounding
        DefensiveReboundProblem,

        // Defense
        OpponentHotFromThree,
        PerimeterDefenseProblem,
        InteriorDefenseProblem,
        TransitionDefenseProblem,
        FoulsProblem,

        // Game control
        PaceControlProblem,

        // Offense (Milestone 3) - appended rather than grouped with the other
        // Offense tags above: AnalysisRecord.ProblemTagsJson persists these as raw
        // ordinals (System.Text.Json's default enum encoding, read back via
        // ProblemTagDto.MapTag(int) in Admin), so inserting a new member earlier in
        // this list would silently reassign the ordinals - and therefore the
        // meaning - of every tag after it in already-persisted analysis records.
        // New tags must always be appended here, never inserted.
        //
        // The team's effective field-goal percentage (from the Milestone 2
        // calculated-metrics layer) is low, with enough field-goal attempts for the
        // percentage to be meaningful. Describes the measured shooting result only -
        // it does not assert a cause (shot selection, spacing, defense, etc.).
        LowEffectiveFieldGoalPercentage
    }

    public enum SuggestionCategory
    {
        Offense,
        Defense,
        Other
    }
}
