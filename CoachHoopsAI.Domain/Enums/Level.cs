
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

        // Legacy identifier name, kept for API/persistence stability (see the
        // ordinal-safety note below) even though it now overstates what the
        // trigger establishes. High three-point share (ThreePointAttemptRate)
        // combined with a below-threshold ThreePointPercentage is a
        // coaching-judgment signal worth a look - it is NOT proof that a
        // different shot mix would score more: e.g. 30% from three is 0.9 points
        // per attempt, which can still exceed that team's actual two-point
        // efficiency, and this rule has no two-point value to compare against.
        // The Admin-facing label (ProblemTagDto) is phrased as an observation
        // ("High Three Point Share With Low Three Point Percentage"), not this
        // enum member's "too many" framing. See Docs/03-domain-and-rules.md.
        TooManyThreePointAttempts,

        // Retired as of ruleset 1.3 - StatRulesEngine no longer triggers this tag.
        // Its original trigger (team personal fouls far below the opponent's, while
        // not leading on score) had no defensible connection to interior/rim
        // aggression; TeamStats has no shot-location data (no PointsInPaint or
        // equivalent) to support a real "paint pressure" finding. Kept defined,
        // never removed, so already-persisted analysis records containing this
        // ordinal keep displaying correctly (see the ordinal-safety note below).
        // See LowFreeThrowRate for the replacement finding and
        // Docs/03-domain-and-rules.md for the full rationale.
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
        LowEffectiveFieldGoalPercentage,

        // Replaces LackOfPaintPressure above. The team attempted few free throws
        // relative to its field-goal attempts (FTA/FGA, Milestone 2A), with enough
        // field-goal attempts for the rate to be meaningful. This is a literal
        // description of that rate only - it does NOT claim, prove, or imply low
        // interior/rim aggression, paint pressure, shot attempts in the paint, or
        // shot selection. Free throws arise from several situations besides paint
        // drives (post-ups away from the basket, and-one calls, deliberate late-game
        // fouling, shooting fouls beyond the paint), and real paint attacks often
        // draw no whistle at all, so this rate cannot stand in for paint activity;
        // TeamStats also has no shot-location data to support that claim directly.
        LowFreeThrowRate
    }

    public enum SuggestionCategory
    {
        Offense,
        Defense,
        Other
    }
}
