using CoachHoopsAI.Domain.Compatibility;
using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using CoachHoopsAI.Domain.Metrics;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Domain.Rules
{
    public class StatRulesEngine : IStatRulesEngine
    {
        public IReadOnlyCollection<ProblemTag> Evaluate(TeamStats team, TeamStats opponent, RulesProfile profile)
        {
            profile ??= new RulesProfile();

            var tags = new List<ProblemTag>();

            var teamFieldGoalPct = LegacyPercentageBridge.FieldGoalPercentage(team);
            var teamThreePointPct = LegacyPercentageBridge.ThreePointPercentage(team);

            // Milestone 3's rules that read the Milestone 2 calculated-metrics layer
            // directly, instead of LegacyPercentageBridge. GameCalculatedMetricsCalculator
            // (M2B), not just CalculatedMetricsCalculator (M2A), is used here because
            // OffensiveEfficiencyProblem below needs OffensiveRating/EstimatedPossessions,
            // which only M2B populates - .Team and .Opponent already carry every M2A
            // field too (unchanged from a direct CalculatedMetricsCalculator.Calculate(...)
            // call on each side), so this single call covers both without duplicating
            // either calculator.
            var gameMetrics = GameCalculatedMetricsCalculator.Calculate(team, opponent);
            var teamMetrics = gameMetrics.Team;

            // Offense
            if ((team.Turnovers - opponent.Turnovers) >= profile.TurnoverDiffToFlag)
                tags.Add(ProblemTag.TurnoverProblem);

            if (teamThreePointPct <= profile.OurBadThreePct && team.ThreePointsAttempted >= profile.OurBadThreeAttemptsMin)
                tags.Add(ProblemTag.OurShootingInefficiency);

            // Minimum-attempts gate is required, not optional: CalculatedMetricsCalculator
            // returns EffectiveFieldGoalPercentage == 0.0 when FieldGoalsAttempted == 0
            // (Milestone 2A's zero-denominator convention), which would otherwise read as
            // "maximally inefficient" for a team that simply hasn't shot yet.
            if (team.FieldGoalsAttempted >= profile.OurLowEffectiveFieldGoalPctAttemptsMin
                && teamMetrics.EffectiveFieldGoalPercentage <= profile.OurLowEffectiveFieldGoalPct)
                tags.Add(ProblemTag.LowEffectiveFieldGoalPercentage);

            // Replaces the old LackOfPaintPressure trigger below (personal fouls +
            // score), which had no defensible connection to what it claimed to
            // measure. FreeThrowRate == 0.0 when FieldGoalsAttempted == 0 (same M2A
            // convention as above), so the same kind of attempts gate applies here.
            if (team.FieldGoalsAttempted >= profile.OurLowFreeThrowRateAttemptsMin
                && teamMetrics.FreeThrowRate <= profile.OurLowFreeThrowRate)
                tags.Add(ProblemTag.LowFreeThrowRate);

            // Milestone 3 refinement: previously gated volume on an ABSOLUTE 3PA
            // count, which meant the same raw count implied something different
            // depending on total shot volume (30 of 60 shots is a very different
            // diet from 30 of 100). Now reads ThreePointAttemptRate (3PA/FGA, M2A)
            // directly, so the volume side is relative to the team's own shot diet.
            // The ThreePointPercentage gate (TooManyThreePctMax) is unchanged, but
            // does NOT prove this shot mix scores worse than the alternative: e.g.
            // 30% from three is 0.9 points per attempt, which can still beat that
            // team's actual two-point efficiency - this rule has no two-point value
            // to compare against, so it never makes that comparison. The trigger is
            // a coaching-judgment threshold, not proof a different mix would score
            // more (see Docs/03-domain-and-rules.md); ProblemTag.TooManyThreePointAttempts
            // is a legacy identifier kept for API/persistence stability, while the
            // Admin-facing label is phrased as an observation, not a verdict. Also
            // migrated ThreePointPercentage off LegacyPercentageBridge onto
            // teamMetrics (M2A) - identical formula, no behavior change from that
            // switch alone.
            if (team.FieldGoalsAttempted >= profile.TooManyThreeAttemptRateAttemptsMin
                && teamMetrics.ThreePointAttemptRate >= profile.TooManyThreeAttemptRateMin
                && teamMetrics.ThreePointPercentage <= profile.TooManyThreePctMax)
                tags.Add(ProblemTag.TooManyThreePointAttempts);

            // Milestone 3 refinement: previously required BOTH losing by a score
            // margin AND a low raw FG% - being behind was never necessary for an
            // offense to be inefficient, and raw FG% ignores turnovers and free
            // throws entirely. Now reads OffensiveRating (points per 100 estimated
            // possessions, M2B) directly, unconditional on score. Reuses this same
            // ProblemTag rather than a new one: the old trigger was a narrow,
            // score-gated proxy for the same underlying concept this measures
            // directly, not something conceptually unrelated (contrast
            // LackOfPaintPressure's retirement above) - see
            // Docs/03-domain-and-rules.md. Possessions-min gate (not just a null
            // check) is required: OffensiveRating is null only when
            // EstimatedPossessions == 0 exactly, but a non-zero, invalid negative
            // possession estimate would otherwise produce a non-null, meaningless
            // rating - PossessionsMin being a positive threshold excludes zero,
            // negative, and merely-too-small samples in one comparison.
            if (teamMetrics.EstimatedPossessions >= profile.OurLowOffensiveRatingPossessionsMin
                && teamMetrics.OffensiveRating.HasValue
                && teamMetrics.OffensiveRating.Value <= profile.OurLowOffensiveRating)
                tags.Add(ProblemTag.OffensiveEfficiencyProblem);

            // Defense
            // Retired as of ruleset 1.8 - StatRulesEngine no longer triggers
            // InteriorDefenseProblem. Its old trigger (opponentFieldGoalPct >=
            // OpponentHighFieldGoalPct, opponentFieldGoalPct being the opponent's raw,
            // unweighted FG% across every shot type combined) had NO minimum-attempts
            // gate at all - the weakest sample protection of any rule in this method -
            // and TeamStats has no shot-location data to support an "interior" defense
            // finding specifically. Replaced below by
            // HighOpponentEffectiveFieldGoalPercentage, which reads the opponent's
            // EffectiveFieldGoalPercentage (M2A via gameMetrics.Opponent) instead - the
            // same eFG% formula used for our own LowEffectiveFieldGoalPercentage above,
            // gated on a real FieldGoalsAttempted minimum. See
            // Docs/03-domain-and-rules.md for the full rationale.
            if (opponent.FieldGoalsAttempted >= profile.OpponentHighEffectiveFieldGoalPctAttemptsMin
                && gameMetrics.Opponent.EffectiveFieldGoalPercentage >= profile.OpponentHighEffectiveFieldGoalPct)
                tags.Add(ProblemTag.HighOpponentEffectiveFieldGoalPercentage);

            // Milestone 3: migrated the percentage side off LegacyPercentageBridge onto
            // gameMetrics.Opponent.ThreePointPercentage (M2A, via M2B above) - an
            // identical formula (ThreePointsMade / ThreePointsAttempted, 0.0 when
            // ThreePointsAttempted is zero), so this is a mechanically equivalent
            // migration with no behavior change: same trigger, same thresholds, same
            // tag, no RulesetVersion bump. The trigger itself was already sound and is
            // unchanged - a measured shooting result gated on a minimum attempts
            // sample (OpponentHotThreeAttemptsMin), not an inferred tactical cause; see
            // Docs/03-domain-and-rules.md.
            if (gameMetrics.Opponent.ThreePointPercentage >= profile.OpponentHotThreePct && opponent.ThreePointsAttempted >= profile.OpponentHotThreeAttemptsMin)
                tags.Add(ProblemTag.OpponentHotFromThree);

            // Retired as of ruleset 1.7 - StatRulesEngine no longer triggers
            // PerimeterDefenseProblem. Its old trigger
            // (opponentThreePointPct >= 0.36 && opponent.ThreePointsAttempted >=
            // team.ThreePointsAttempted + 5) read exactly the same underlying evidence
            // as OpponentHotFromThree above (opponent three-point percentage and
            // volume) but wrapped it in a "perimeter defense" tactical diagnosis -
            // closeouts, rotations, communication, contest quality, defensive
            // positioning - none of which TeamStats can establish; opponent 3P% alone
            // only measures a shooting result, not why it happened. Its volume gate
            // was also weaker than OpponentHotFromThree's real per-level minimum: as
            // few as 5 total opponent attempts (if our own team hadn't shot a three
            // yet) satisfied it, not a meaningful sample for a percentage. Its
            // threshold bypassed RulesProfile entirely too (hardcoded 0.36, not
            // tunable per level, unlike every other threshold in this method). Rather
            // than reuse the tag with a corrected trigger - which would just restate
            // OpponentHotFromThree's signal under a different, causally-loaded name -
            // it is retired outright, with no replacement tag: OpponentHotFromThree
            // above already is the single, literal, profile-tunable observation of
            // this evidence. The enum member and Admin mapping are kept, never
            // removed, so already-persisted analysis records keep displaying
            // correctly. See Docs/03-domain-and-rules.md.

            // Retired as of ruleset 1.9 - StatRulesEngine no longer triggers
            // TransitionDefenseProblem. Its old trigger
            // (opponent.Points - team.Points >= LossByPointsToFlagTransition &&
            // team.Turnovers >= TurnoversMinToFlagTransition) read only team
            // turnovers and score margin. TeamStats has no fast-break points,
            // points-off-turnovers, live/dead-ball turnover distinction, possession
            // sequencing, or shot-timing data - nothing that connects a turnover to
            // the opponent actually scoring off it, let alone in transition
            // specifically, and nothing that isolates "our transition defense" as
            // the cause of a score deficit rather than any of the many other
            // explanations (cold shooting, a hot opponent, foul trouble, etc.). The
            // only measurable fact in the old trigger - elevated team turnovers - is
            // already flagged by TurnoverProblem above (a differential-based,
            // already-reviewed rule); this trigger added nothing but an unsupported
            // causal label on top of that same signal plus a score-margin condition
            // that supplies no missing evidence. Retired outright, no replacement
            // tag, same pattern as PerimeterDefenseProblem above: the measurable
            // part is redundant with an existing finding, not a distinct signal
            // worth a new tag. See Docs/03-domain-and-rules.md for the full
            // rationale and what data a real transition-defense finding would need.
            // Milestone 3 refinement: the original differential-only trigger
            // (preserved below, unchanged) can hide a real foul problem when both
            // teams foul heavily - e.g. team 20 fouls, opponent 17, diff 3, never
            // fired despite a high absolute total. Added a second, independent
            // absolute-count condition (FoulsHighCountToFlag) that fires regardless
            // of the opponent's total, catching that case without touching the
            // differential's existing behavior - this is a strict expansion, not a
            // replacement: everything that triggered before still triggers.
            // Deliberately stays in plain foul-count terms rather than migrating to
            // FoulRate (M2B, fouls per opponent estimated possession): a coach
            // reads "18 fouls" far more naturally than an abstract per-possession
            // rate, FoulRate's denominator has the same non-positive-possessions
            // edge case OffensiveEfficiencyProblem already has to guard against,
            // and the demonstrated gap here is fully solved within the count
            // domain, so introducing a rate adds complexity without a clear
            // benefit (see Docs/03-domain-and-rules.md). The differential branch
            // is UNCHANGED and retained as-is for backward compatibility and
            // because it still detects a real relative imbalance - it is NOT
            // normalized by elapsed game time or possessions (StatRulesEngine.Evaluate
            // has no GameFormat/GameTiming access at all), so it still permits an
            // early 5-0 foul read to trigger FoulsProblem at Amateur level exactly
            // as before this refinement. That is a retained limitation, not sample
            // protection: unlike a small-attempts percentage (mathematically
            // distorted, e.g. 1-for-1 reading as a "perfect" 100%), a plain count
            // isn't distorted by a small sample - 5 fouls is exactly 5 fouls - but
            // this trigger still cannot tell an early, thin read from a full-game
            // one; early-live-game confidence remains an open, unresolved concern
            // (see Docs/03-domain-and-rules.md and CLAUDE.md). No score-margin
            // input is used, by design: this finding describes a measured foul
            // count only, not why the fouls happened (positioning, rotations,
            // discipline, officiating, aggression) or whether the score is related.
            if ((team.PersonalFouls - opponent.PersonalFouls) >= profile.FoulsDiffToFlag
                || team.PersonalFouls >= profile.FoulsHighCountToFlag)
                tags.Add(ProblemTag.FoulsProblem);


            // Rebounding
            // Milestone 3 refinement: previously compared opponent.OffensiveRebounds
            // to team.OffensiveRebounds - two OFFENSIVE rebound counts, neither of
            // which is our defensive rebounding. team.DefensiveRebounds was never
            // read at all, so a team with a genuine defensive-rebounding problem
            // could dodge the old flag simply by also offensive-rebounding well
            // (which shrinks the differential but says nothing about defense).
            // Retired below (DefensiveReboundProblem no longer triggers) rather than
            // reused, since the old data had no defensible connection to what the
            // tag claims - contrast OffensiveEfficiencyProblem/TooManyThreePointAttempts
            // above, whose old triggers at least read the right data, just imprecisely.
            // LowDefensiveReboundPercentage replaces it, reading
            // DefensiveReboundPercentage (TeamDREB / (TeamDREB + OpponentOREB), M2B)
            // directly: the share of available defensive-rebound opportunities - our
            // own defensive rebounds plus the opponent's offensive rebounds - that we
            // actually secured. Opportunities-min gate mirrors OffensiveEfficiencyProblem's
            // PossessionsMin reasoning, though this denominator (a sum of two
            // non-negative counts) can only be non-positive when both terms are
            // exactly zero, unlike EstimatedPossessions' subtraction.
            if (team.DefensiveRebounds + opponent.OffensiveRebounds >= profile.OurLowDefensiveReboundPctOpportunitiesMin
                && teamMetrics.DefensiveReboundPercentage.HasValue
                && teamMetrics.DefensiveReboundPercentage.Value <= profile.OurLowDefensiveReboundPct)
                tags.Add(ProblemTag.LowDefensiveReboundPercentage);


            // Game Control
            if (Math.Abs(team.Points - opponent.Points) >= 15)
                tags.Add(ProblemTag.PaceControlProblem);


            return tags;
        }
    }
}
