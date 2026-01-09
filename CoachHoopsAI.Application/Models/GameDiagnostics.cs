using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachHoopsAI.Application.Models
{
    public class GameDiagnostics
    {
        public int PointsDiff { get; }
        public int TurnoversDiff { get; }

        public int OffensiveReboundsDiff { get; }
        public int DefensiveReboundsDiff { get; }

        public double ThreePointPctDiff { get; }
        public int ThreePointAttemptsDiff { get; }

        public int FoulsDiff { get; }

        public double TeamFieldGoalPercentage { get; }
        public double OpponentFieldGoalPercentage { get; }
        public double FieldGoalPctDiff { get; }

        public string AppliedRulesProfile { get; }

        public GameDiagnostics(
            int pointsDiff,
            int turnoversDiff,
            int offensiveReboundsDiff,
            int defensiveReboundsDiff,
            double threePointPctDiff,
            int threePointAttemptsDiff,
            int foulsDiff,
            double teamFieldGoalPercentage,
            double opponentFieldGoalPercentage,
            double fieldGoalPctDiff,
            string appliedRulesProfile)
        {
            PointsDiff = pointsDiff;
            TurnoversDiff = turnoversDiff;
            OffensiveReboundsDiff = offensiveReboundsDiff;
            DefensiveReboundsDiff = defensiveReboundsDiff;
            ThreePointPctDiff = threePointPctDiff;
            ThreePointAttemptsDiff = threePointAttemptsDiff;
            FoulsDiff = foulsDiff;
            TeamFieldGoalPercentage = teamFieldGoalPercentage;
            OpponentFieldGoalPercentage = opponentFieldGoalPercentage;
            FieldGoalPctDiff = fieldGoalPctDiff;
            AppliedRulesProfile = appliedRulesProfile;
        }
    }
}
