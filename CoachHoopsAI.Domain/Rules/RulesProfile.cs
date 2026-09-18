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

        // Rebounding
        public int OpponentOffensiveReboundDiffToFlag { get; set; } = 5;

        // Three-point defense
        public double OpponentHotThreePct { get; set; } = 0.38;
        public int OpponentHotThreeAttemptsMin { get; set; } = 20;

        // Three-point offense
        public double OurBadThreePct { get; set; } = 0.30;
        public int OurBadThreeAttemptsMin { get; set; } = 15;

        public int TooManyThreeAttemptsMin { get; set; } = 30;
        public double TooManyThreePctMax { get; set; } = 0.33;

        // Fouls
        public int FoulsDiffToFlag { get; set; } = 5;

        // “Interior defense” proxy
        public double OpponentHighFieldGoalPct { get; set; } = 0.52;

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
    }
}
