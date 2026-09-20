using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachHoopsAI.Domain.Rules
{
    public class RulesProfile
    {
        // Turnovers
        public int TurnoverDiffToFlag { get; set; } = 5;

        // Three-point defense
        public double OpponentHotThreePct { get; set; } = 0.38;
        public int OpponentHotThreeAttemptsMin { get; set; } = 20;

        // Three-point offense
        public double OurBadThreePct { get; set; } = 0.30;
        public int OurBadThreeAttemptsMin { get; set; } = 15;

        // Milestone 3 refinement: the volume side of this finding is now a rate
        // (3PA / FGA, M2A ThreePointAttemptRate) rather than an absolute count -
        // the same raw 3PA count means something different depending on total shot
        // volume (30 of 60 shots is a very different diet from 30 of 100).
        // AttemptsMin (a FieldGoalsAttempted minimum, since the rate's denominator
        // is FGA) mirrors the other M3 attempts-gates' per-level values and
        // reasoning: a few early shots can't read as a high share from a trivial
        // sample. TooManyThreePctMax (below) is unchanged and still required, but
        // note what it does NOT establish: a high 3PA rate combined with a 3P% at
        // or below this threshold is a coaching-judgment signal worth a look, not
        // proof that a different shot mix would score more. E.g. 30% from three is
        // 0.9 points per attempt, which can still exceed that same team's actual
        // two-point efficiency - this rule has no data on two-point value to
        // compare against, so it never makes that comparison. The
        // ProblemTag.TooManyThreePointAttempts identifier is a legacy name kept
        // for API/persistence stability; the coach-facing label
        // (`ProblemTagDto`) is deliberately phrased as an observation, not a
        // verdict - see Docs/03-domain-and-rules.md.
        public double TooManyThreeAttemptRateMin { get; set; } = 0.40;
        public int TooManyThreeAttemptRateAttemptsMin { get; set; } = 20;
        public double TooManyThreePctMax { get; set; } = 0.33;

        // Fouls
        public int FoulsDiffToFlag { get; set; } = 5;

        // Transition defense proxy
        public int LossByPointsToFlagTransition { get; set; } = 10;
        public int TurnoversMinToFlagTransition { get; set; } = 15;

        // Overall shooting efficiency (effective field-goal %, Milestone 3) - current
        // defaults, not universal basketball facts. eFG% credits three-pointers at
        // 1.5x a two-pointer (matching CalculatedMetricsCalculator). AttemptsMin
        // exists so a handful of early-game attempts can't read as "inefficient"
        // purely from a small-sample percentage.
        public double OurLowEffectiveFieldGoalPct { get; set; } = 0.47;
        public int OurLowEffectiveFieldGoalPctAttemptsMin { get; set; } = 20;

        // Offensive rating (points per 100 estimated possessions, Milestone 3;
        // GameCalculatedMetricsCalculator/M2B) - current defaults, not universal
        // basketball facts. Replaces OffensiveEfficiencyProblem's old trigger (raw
        // FG% gated on losing by a score margin - LossByPointsToFlagOffensiveEfficiency
        // and OurLowFieldGoalPctForOffensiveEfficiency, both removed): being behind is
        // not required for an offense to be inefficient, and points-per-possession
        // accounts for turnovers and free throws, which raw FG% ignores entirely.
        // PossessionsMin (a possession count, not an attempts count, since
        // OffensiveRating's denominator is EstimatedPossessions) exists for the same
        // small-sample reason as the AttemptsMin fields above, and mirrors their
        // per-level values since both gate a possession-scale sample size. A low
        // rating here is a measured result only - it does not identify a tactical
        // cause (shot selection, pace, turnovers, etc.).
        public double OurLowOffensiveRating { get; set; } = 95.0;
        public double OurLowOffensiveRatingPossessionsMin { get; set; } = 20.0;

        // Free-throw rate (FTA/FGA, Milestone 3) - current defaults, not universal
        // basketball facts. Replaces the old LackOfPaintPressure heuristic
        // (personal fouls + score), which read raw counts with no defensible
        // connection to the concept it claimed to measure. This is a literal rate,
        // not an interior-aggression or paint-pressure claim: free throws arise from
        // several situations besides paint drives, and real paint attacks often draw
        // no whistle at all, so a low rate here does not by itself say why (shot
        // selection, spacing, whistle tendencies at this level, etc.) or confirm
        // anything about where shots were taken. AttemptsMin mirrors
        // OurLowEffectiveFieldGoalPctAttemptsMin's reasoning and per-level scaling:
        // a handful of early-game attempts can't read as "low" purely from a
        // small-sample rate.
        public double OurLowFreeThrowRate { get; set; } = 0.18;
        public int OurLowFreeThrowRateAttemptsMin { get; set; } = 20;

        // Defensive rebound percentage (Milestone 3; GameCalculatedMetricsCalculator/
        // M2B) - current defaults, not universal basketball facts. Replaces
        // DefensiveReboundProblem's old trigger (OpponentOffensiveReboundDiffToFlag,
        // removed - opponent.OffensiveRebounds minus team.OffensiveRebounds, which
        // never read team.DefensiveRebounds at all). This reads
        // DefensiveReboundPercentage directly: our defensive rebounds divided by
        // defensive-rebound opportunities (our defensive rebounds plus the
        // opponent's offensive rebounds). OpportunitiesMin exists for the same
        // small-sample reason as the AttemptsMin/PossessionsMin fields above, and
        // mirrors their per-level values, since all gate a comparably-sized raw
        // sample; unlike EstimatedPossessions (a subtraction that can go negative
        // even on valid non-negative input), this denominator is a sum of two
        // non-negative counts, so it can only be non-positive when both are exactly
        // zero - OpportunitiesMin still protects against a real but trivial early
        // live-game sample reading as a stable percentage. A low percentage here is
        // a measured result only - it does not identify a tactical cause (box-out
        // technique, positioning, effort, etc.); TeamStats has no data to support
        // attributing one.
        public double OurLowDefensiveReboundPct { get; set; } = 0.65;
        public int OurLowDefensiveReboundPctOpportunitiesMin { get; set; } = 20;

        // Opponent's overall effective field-goal percentage (Milestone 3;
        // GameCalculatedMetricsCalculator/M2A via M2B) - newly chosen project
        // defaults, not universal basketball facts and not a reuse of the retired
        // InteriorDefenseProblem trigger's numbers. Replaces that old trigger
        // (OpponentHighFieldGoalPct, removed - the opponent's raw, unweighted FG%
        // across every shot type combined, with NO minimum-attempts gate at all).
        // This reads the opponent's EffectiveFieldGoalPercentage directly - the same
        // eFG% formula as OurLowEffectiveFieldGoalPct above, crediting three-pointers
        // at 1.5x a two-pointer. eFG% is systematically higher than raw FG% for any
        // team with real three-point volume, and that gap widens with how much of
        // the shot diet is threes - reusing the old raw-FG%-calibrated numbers as-is
        // would have made this trigger fire more easily than the retired rule did
        // for equivalent-quality shooting, which is not the intent. The five
        // per-level values (see CoachHoopsAI.Api/appsettings.json) were chosen
        // deliberately for eFG%'s scale instead, each a few points above the old
        // field's corresponding value, sized by how much three-point volume is
        // realistic at that level: EasyBasket's bump is smallest (young players
        // rarely shoot threes, so eFG% and raw FG% barely diverge there), Pro's
        // bump is largest (the highest-volume, highest-value three-point shooting),
        // and Youth/Amateur_Development/Amateur fall in between - see
        // Docs/03-domain-and-rules.md for the exact values and the full rationale.
        // AttemptsMin mirrors OurLowEffectiveFieldGoalPctAttemptsMin's per-level
        // values and reasoning (same metric family, gated on the same
        // FieldGoalsAttempted denominator) - the old trigger had no equivalent gate
        // at all, so a single early shot could previously read as a stable
        // percentage. A high percentage here is a measured shooting-efficiency
        // result only - it does not identify a defensive cause (rim protection,
        // rotations, closeouts, positioning, effort); TeamStats has no data to
        // support attributing one. Distinct from OpponentHotThreePct/
        // OpponentHotThreeAttemptsMin above, which read three-point shooting
        // specifically - a team can trip one without the other.
        public double OpponentHighEffectiveFieldGoalPct { get; set; } = 0.56;
        public int OpponentHighEffectiveFieldGoalPctAttemptsMin { get; set; } = 20;
    }
}
