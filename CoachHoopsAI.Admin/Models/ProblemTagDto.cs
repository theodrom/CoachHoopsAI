namespace CoachHoopsAI.Admin.Models;

public sealed class ProblemTagDto
{
    public int Tag { get; set; }
    public override string ToString() => MapTag(Tag);

    public static string MapTag(int value) => value switch
    {
        0 => "None",
        1 => "Turnover Problem",
        2 => "Offensive Efficiency Problem",
        3 => "Our Shooting Inefficiency",
        4 => "Too Many Three Point Attempts",
        5 => "Lack Of Paint Pressure", // retired ruleset 1.3 onward - StatRulesEngine no longer triggers this; kept for historical records
        6 => "Defensive Rebound Problem",
        7 => "Opponent Hot From Three",
        8 => "Perimeter Defense Problem",
        9 => "Interior Defense Problem",
        10 => "Transition Defense Problem",
        11 => "Fouls Problem",
        12 => "Pace Control Problem",
        13 => "Low Effective Field Goal Percentage",
        14 => "Low Free Throw Rate",
        _ => $"Unknown({value})"
    };
}
