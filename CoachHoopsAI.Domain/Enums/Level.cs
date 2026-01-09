using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        PaceControlProblem
    }

    public enum SuggestionCategory
    {
        Offense,
        Defense,
        Other
    }
}
