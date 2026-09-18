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

        // Offensive efficiency proxy
        public int LossByPointsToFlagOffensiveEfficiency { get; set; } = 10;
        public double OurLowFieldGoalPctForOffensiveEfficiency { get; set; } = 0.45;

        // Transition defense proxy
        public int LossByPointsToFlagTransition { get; set; } = 10;
        public int TurnoversMinToFlagTransition { get; set; } = 15;

        // Overall shooting efficiency (effective field-goal %, Milestone 3) - current
        // defaults, not universal basketball facts. eFG% credits three-pointers at
        // 1.5x a two-pointer (matching CalculatedMetricsCalculator), so this
        // threshold sits a little above the raw-FG%-based
        // OurLowFieldGoalPctForOffensiveEfficiency threshold above, since a team
        // that makes some threes will always read a few points higher on eFG% than
        // on raw FG%. AttemptsMin exists so a handful of early-game attempts can't
        // read as "inefficient" purely from a small-sample percentage.
        public double OurLowEffectiveFieldGoalPct { get; set; } = 0.47;
        public int OurLowEffectiveFieldGoalPctAttemptsMin { get; set; } = 20;
    }
}
