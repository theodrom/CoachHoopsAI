namespace CoachHoopsAI.Admin.Models;

public sealed class ProblemTagDto
{
    public int Tag { get; set; }
    public override string ToString() => MapTag(Tag);

    public static string MapTag(int value) => value switch
    {
        0 => "None",
        1 => "TurnoverProblem",
        2 => "OffensiveEfficiencyProblem",
        3 => "OurShootingInefficiency",
        4 => "TooManyThreePointAttempts",
        5 => "LackOfPaintPressure",
        6 => "DefensiveReboundProblem",
        7 => "OpponentHotFromThree",
        8 => "PerimeterDefenseProblem",
        9 => "InteriorDefenseProblem",
        10 => "TransitionDefenseProblem",
        11 => "FoulsProblem",
        12 => "PaceControlProblem",
        _ => $"Unknown({value})"
    };
}
