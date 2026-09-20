
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

        // Retired as of ruleset 1.6 - StatRulesEngine no longer triggers this tag.
        // Its original trigger (opponent.OffensiveRebounds - team.OffensiveRebounds
        // >= threshold) compared two OFFENSIVE rebound counts across teams; it never
        // read team.DefensiveRebounds at all, so it had no defensible connection to
        // our own defensive rebounding - a team with genuinely poor defensive
        // rebounding could avoid the flag simply by also offensive-rebounding well
        // (which lowers the differential but says nothing about defense), and vice
        // versa. Kept defined, never removed, so already-persisted analysis records
        // containing this ordinal keep displaying correctly (see the ordinal-safety
        // note below). See LowDefensiveReboundPercentage for the replacement finding
        // and Docs/03-domain-and-rules.md for the full rationale.
        DefensiveReboundProblem,

        // Defense
        OpponentHotFromThree,

        // Retired as of ruleset 1.7 - StatRulesEngine no longer triggers this tag.
        // Its original trigger (opponent three-point percentage >= a hardcoded 0.36,
        // gated on opponent.ThreePointsAttempted >= team.ThreePointsAttempted + 5)
        // read exactly the same evidence as OpponentHotFromThree above - opponent
        // three-point shooting percentage and volume - but labeled it a "perimeter
        // defense" tactical diagnosis; TeamStats has no closeout, rotation,
        // communication, contest-quality, or positioning data to support that
        // causal claim, and its volume gate (as few as 5 opponent attempts) was
        // weaker than a meaningful sample. Retired outright rather than reused or
        // replaced, since OpponentHotFromThree already is the literal,
        // profile-tunable observation of this same evidence. Kept defined, never
        // removed, so already-persisted analysis records containing this ordinal
        // keep displaying correctly (see the ordinal-safety note below). See
        // Docs/03-domain-and-rules.md for the full rationale.
        PerimeterDefenseProblem,

        // Retired as of ruleset 1.8 - StatRulesEngine no longer triggers this tag.
        // Its original trigger (opponent's raw, unweighted field-goal percentage
        // across every shot type combined >= OpponentHighFieldGoalPct, with NO
        // minimum field-goal-attempts gate at all - the weakest sample protection
        // of any rule in StatRulesEngine, worse than PerimeterDefenseProblem's own
        // trivial gate) read overall shooting efficiency, not any paint- or
        // rim-specific data; TeamStats has no shot-location data (no PointsInPaint,
        // rim-attempt count, or shot-distance breakdown) to support an "interior"
        // defense finding specifically - the same limitation that retired
        // LackOfPaintPressure and PerimeterDefenseProblem. Unlike those two,
        // though, the underlying signal (the opponent's overall shooting
        // efficiency) was not already covered elsewhere - OpponentHotFromThree only
        // reads three-point shooting - so it was replaced rather than retired
        // outright. See HighOpponentEffectiveFieldGoalPercentage below for the
        // neutral replacement. Kept defined, never removed, so already-persisted
        // analysis records containing this ordinal keep displaying correctly (see
        // the ordinal-safety note below). See Docs/03-domain-and-rules.md for the
        // full rationale.
        InteriorDefenseProblem,

        // Retired as of ruleset 1.9 - StatRulesEngine no longer triggers this tag.
        // Its original trigger (opponent leading by a score margin AND team
        // turnovers at or above an absolute count) read only score and team
        // turnovers; TeamStats has no fast-break points, points-off-turnovers,
        // live/dead-ball turnover distinction, possession sequencing, or
        // shot-timing data to connect a turnover to the opponent scoring off it,
        // let alone in transition specifically, or to isolate "our transition
        // defense" as the cause of a score deficit. The only measurable fact in
        // the old trigger - elevated team turnovers - is already covered by
        // TurnoverProblem above; retired outright rather than reused or replaced,
        // since a corrected trigger would just restate TurnoverProblem's signal
        // under a causally-loaded name, the same pattern as PerimeterDefenseProblem
        // above. Kept defined, never removed, so already-persisted analysis
        // records containing this ordinal keep displaying correctly (see the
        // ordinal-safety note below). See Docs/03-domain-and-rules.md for the full
        // rationale and what data a real transition-defense finding would need.
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
        LowFreeThrowRate,

        // Replaces DefensiveReboundProblem above. The team secured a low share of
        // available defensive-rebound opportunities - our own defensive rebounds
        // plus the opponent's offensive rebounds (DefensiveReboundPercentage,
        // Milestone 2B) - with enough opportunities for the share to be meaningful.
        // This is a literal description of that share only - it does NOT claim or
        // imply a specific cause such as poor positioning, boxing-out technique, or
        // lack of effort. TeamStats has no positioning/assignment data to support
        // attributing a cause.
        LowDefensiveReboundPercentage,

        // Replaces InteriorDefenseProblem above. The opponent's overall effective
        // field-goal percentage (EffectiveFieldGoalPercentage - FGM plus half of 3PM,
        // divided by FGA, Milestone 2A via M2B) is high, with enough field-goal
        // attempts for the percentage to be meaningful. This is a literal
        // description of the opponent's overall shooting efficiency only - it does
        // NOT claim or imply a specific defensive cause such as weak rim protection,
        // blown rotations, poor closeouts, or defensive positioning; TeamStats has
        // no shot-location, assignment, or possession-by-possession data to support
        // attributing one. Distinct from OpponentHotFromThree: that tag reads the
        // opponent's three-point shooting specifically, while this one reads their
        // efficiency across their entire shot profile, weighted for shot value - a
        // team can trigger one without the other.
        HighOpponentEffectiveFieldGoalPercentage
    }

    public enum SuggestionCategory
    {
        Offense,
        Defense,
        Other
    }
}
