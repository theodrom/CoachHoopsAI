namespace CoachHoopsAI.Admin.Models;

public sealed class ProblemTagDto
{
    public int Tag { get; set; }
    public override string ToString() => MapTag(Tag);

    public static string MapTag(int value) => value switch
    {
        0 => "None",
        1 => "Turnover Problem",
        2 => "Offensive Efficiency Problem", // ruleset 1.4 onward: OffensiveRating-based, no score-margin gate (see Docs/03-domain-and-rules.md)
        3 => "Our Shooting Inefficiency",
        // Coach-facing label deliberately does not say "too many": a high
        // three-point share combined with a below-threshold 3P% does not by
        // itself prove that fewer three-point attempts would have scored more
        // (see Docs/03-domain-and-rules.md). ProblemTag.TooManyThreePointAttempts
        // (the code/ordinal, unchanged since ruleset 1.5's rate-based refinement)
        // remains the stable identifier for historical records.
        4 => "High Three Point Share With Low Three Point Percentage",
        5 => "Lack Of Paint Pressure", // retired ruleset 1.3 onward - StatRulesEngine no longer triggers this; kept for historical records
        6 => "Defensive Rebound Problem", // retired ruleset 1.6 onward - StatRulesEngine no longer triggers this; kept for historical records
        7 => "Opponent Hot From Three",
        8 => "Perimeter Defense Problem", // retired ruleset 1.7 onward - StatRulesEngine no longer triggers this; kept for historical records
        9 => "Interior Defense Problem", // retired ruleset 1.8 onward - StatRulesEngine no longer triggers this; kept for historical records
        10 => "Transition Defense Problem", // retired ruleset 1.9 onward - StatRulesEngine no longer triggers this; kept for historical records
        11 => "Fouls Problem",
        12 => "Pace Control Problem",
        13 => "Low Effective Field Goal Percentage",
        14 => "Low Free Throw Rate",
        15 => "Low Defensive Rebound Percentage",
        16 => "High Opponent Effective Field Goal Percentage",
        _ => $"Unknown({value})"
    };
}
