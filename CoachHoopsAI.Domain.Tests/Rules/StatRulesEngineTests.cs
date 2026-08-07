using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Rules;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Domain.Tests.Rules;

// Locks down today's StatRulesEngine behavior (12 threshold-based rules against a
// RulesProfile) so it can be safely evolved in a later milestone. Every test uses
// a "healthy" baseline that trips no rules, then perturbs exactly the stat(s) a
// single rule reads so failures point at the rule that broke.
public class StatRulesEngineTests
{
    private readonly StatRulesEngine _engine = new();
    private readonly RulesProfile _profile = new(); // default thresholds

    // Points, FieldGoalPct, ThreePointPct, ThreePointAttempts, OffensiveRebounds, DefensiveRebounds, Turnovers, Fouls
    private static TeamStats Healthy() =>
        new(points: 80, fieldGoalPercentage: 0.50, threePointPercentage: 0.35, threePointAttempts: 20,
            offensiveRebounds: 10, defensiveRebounds: 30, turnovers: 12, fouls: 15);

    private static TeamStats HealthyOpponent() =>
        new(points: 78, fieldGoalPercentage: 0.45, threePointPercentage: 0.30, threePointAttempts: 18,
            offensiveRebounds: 8, defensiveRebounds: 28, turnovers: 14, fouls: 16);

    [Fact]
    public void Evaluate_HealthyStatsForBothTeams_ReturnsNoTags()
    {
        var tags = _engine.Evaluate(Healthy(), HealthyOpponent(), _profile);

        Assert.Empty(tags);
    }

    [Theory]
    [InlineData(4, false)] // team.Turnovers - opponent.Turnovers == TurnoverDiffToFlag - 1
    [InlineData(5, true)]  // == TurnoverDiffToFlag (>=, boundary)
    [InlineData(6, true)]  // > TurnoverDiffToFlag
    public void Evaluate_TurnoverDiff_Boundary(int diff, bool expectTag)
    {
        var opponent = HealthyOpponent();
        var team = new TeamStats(80, 0.50, 0.35, 20, 10, 30, opponent.Turnovers + diff, 15);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.TurnoverProblem));
    }

    [Fact]
    public void Evaluate_BadThreePointShootingWithVolume_TriggersOurShootingInefficiency()
    {
        var team = new TeamStats(80, 0.50, threePointPercentage: 0.25, threePointAttempts: 16, 10, 30, 12, 15);
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.OurShootingInefficiency, tags);
    }

    [Fact]
    public void Evaluate_TooManyLowPercentageThreeAttempts_TriggersTooManyThreePointAttempts()
    {
        var team = new TeamStats(80, 0.50, threePointPercentage: 0.30, threePointAttempts: 32, 10, 30, 12, 15);
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.TooManyThreePointAttempts, tags);
    }

    [Fact]
    public void Evaluate_LosingByEnoughWithLowFieldGoalPct_TriggersOffensiveEfficiencyProblem()
    {
        var team = new TeamStats(points: 70, fieldGoalPercentage: 0.40, 0.35, 20, 10, 30, 12, 15);
        var opponent = new TeamStats(points: 82, 0.45, 0.30, 18, 8, 28, 14, 16); // +12 margin

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.OffensiveEfficiencyProblem, tags);
    }

    [Fact]
    public void Evaluate_FarFewerFoulsWhileNotAheadOnScore_TriggersLackOfPaintPressure()
    {
        var team = new TeamStats(points: 70, 0.50, 0.35, 20, 10, 30, 12, fouls: 10);
        var opponent = new TeamStats(points: 78, 0.45, 0.30, 18, 8, 28, 14, fouls: 16); // team.Fouls <= opp.Fouls - 5

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.LackOfPaintPressure, tags);
    }

    [Theory]
    [InlineData(0.51, false)] // just below OpponentHighFieldGoalPct (0.52)
    [InlineData(0.52, true)]  // at threshold (>=)
    [InlineData(0.55, true)]  // above threshold
    public void Evaluate_OpponentFieldGoalPct_Boundary(double opponentFgPct, bool expectTag)
    {
        var team = Healthy();
        var opponent = new TeamStats(78, opponentFgPct, 0.30, 18, 8, 28, 14, 16);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.InteriorDefenseProblem));
    }

    [Fact]
    public void Evaluate_OpponentHotFromThreeWithVolume_TriggersOpponentHotFromThree()
    {
        var team = Healthy();
        var opponent = new TeamStats(78, 0.45, threePointPercentage: 0.40, threePointAttempts: 22, 8, 28, 14, 16);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.OpponentHotFromThree, tags);
    }

    [Fact]
    public void Evaluate_OpponentAboveHardcodedThreePointRate_TriggersPerimeterDefenseProblem()
    {
        // PerimeterDefenseProblem bypasses RulesProfile: hardcoded opponent 3P% >= 0.36
        // and opponent 3PA >= team 3PA + 5.
        var team = new TeamStats(80, 0.50, 0.35, threePointAttempts: 10, 10, 30, 12, 15);
        var opponent = new TeamStats(78, 0.45, threePointPercentage: 0.37, threePointAttempts: 16, 8, 28, 14, 16);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.PerimeterDefenseProblem, tags);
    }

    [Fact]
    public void Evaluate_LosingByEnoughWithHighTurnovers_TriggersTransitionDefenseProblem()
    {
        var team = new TeamStats(points: 65, 0.50, 0.35, 20, 10, 30, turnovers: 16, fouls: 15);
        var opponent = new TeamStats(points: 80, 0.45, 0.30, 18, 8, 28, 14, 16); // +15 margin

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.TransitionDefenseProblem, tags);
    }

    [Theory]
    [InlineData(4, false)] // team.Fouls - opponent.Fouls == FoulsDiffToFlag - 1
    [InlineData(5, true)]  // == FoulsDiffToFlag (boundary)
    [InlineData(6, true)]
    public void Evaluate_FoulsDiff_Boundary(int diff, bool expectTag)
    {
        var opponent = HealthyOpponent();
        var team = new TeamStats(80, 0.50, 0.35, 20, 10, 30, 12, fouls: opponent.Fouls + diff);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.FoulsProblem));
    }

    [Fact]
    public void Evaluate_OpponentOutreboundsOnOffensiveGlass_TriggersDefensiveReboundProblem()
    {
        var team = new TeamStats(80, 0.50, 0.35, 20, offensiveRebounds: 6, 30, 12, 15);
        var opponent = new TeamStats(78, 0.45, 0.30, 18, offensiveRebounds: 12, 28, 14, 16); // diff = 6 >= 5

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.DefensiveReboundProblem, tags);
    }

    [Theory]
    [InlineData(14, false)] // |margin| just below the hardcoded 15-point threshold
    [InlineData(15, true)]  // at threshold
    [InlineData(20, true)]  // above threshold
    public void Evaluate_PointMargin_Boundary(int margin, bool expectTag)
    {
        var opponent = HealthyOpponent();
        var team = new TeamStats(points: opponent.Points + margin, 0.50, 0.35, 20, 10, 30, 12, 15);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.PaceControlProblem));
    }

    [Fact]
    public void Evaluate_MultipleProblemsAtOnce_ReturnsAllTriggeredTagsWithoutDuplicates()
    {
        // Team commits far more turnovers AND has a big offensive-rebound deficit;
        // opponent also shoots well from the field. Three independent rules should fire.
        var team = new TeamStats(60, 0.40, 0.25, 10, offensiveRebounds: 4, 25, turnovers: 22, fouls: 15);
        var opponent = new TeamStats(70, fieldGoalPercentage: 0.55, 0.30, 15, offensiveRebounds: 12, 28, turnovers: 12, fouls: 16);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.TurnoverProblem, tags);
        Assert.Contains(ProblemTag.InteriorDefenseProblem, tags);
        Assert.Contains(ProblemTag.DefensiveReboundProblem, tags);
        Assert.Equal(tags.Distinct().Count(), tags.Count);
    }
}
