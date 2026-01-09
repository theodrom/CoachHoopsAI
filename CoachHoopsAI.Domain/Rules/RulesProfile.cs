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
    }
}
