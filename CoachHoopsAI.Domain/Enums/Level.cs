
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
