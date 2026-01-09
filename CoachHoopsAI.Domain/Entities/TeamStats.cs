using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachHoopsAI.Domain.Entities
{
    public class TeamStats
    {
        public int Points { get; set; }
        public double FieldGoalPercentage { get; set; }
        public double ThreePointPercentage { get; set; }
        public int ThreePointAttempts { get; set; }
        public int OffensiveRebounds { get; set; }
        public int DefensiveRebounds { get; set; }
        public int Turnovers { get; set; }
        public int Fouls { get; set; }

        public TeamStats(
            int points,
            double fieldGoalPercentage,
            double threePointPercentage,
            int threePointAttempts,
            int offensiveRebounds,
            int defensiveRebounds,
            int turnovers,
            int fouls)
        {
            Points = points;
            FieldGoalPercentage = fieldGoalPercentage;
            ThreePointPercentage = threePointPercentage;
            ThreePointAttempts = threePointAttempts;
            OffensiveRebounds = offensiveRebounds;
            DefensiveRebounds = defensiveRebounds;
            Turnovers = turnovers;
            Fouls = fouls;
        }
    }
}
