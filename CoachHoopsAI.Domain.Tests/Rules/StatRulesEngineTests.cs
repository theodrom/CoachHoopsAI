using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Rules;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Domain.Tests.Rules;

// Locks down today's StatRulesEngine behavior (12 threshold-based rules against a
// RulesProfile) so it can be safely evolved in a later milestone. TeamStats now
// stores only raw made/attempted counts (Milestone 1); every percentage-driven
// scenario below is built from an exact made/attempted ratio rather than a stored
// percentage, per the engine's LegacyPercentageBridge (FGM/FGA, 3PM/3PA).
public class StatRulesEngineTests
{
    private readonly StatRulesEngine _engine = new();
    private readonly RulesProfile _profile = new(); // default thresholds

    // FG 30/60 = 0.50, 3P 7/20 = 0.35 - matches the pre-Milestone-1 "healthy" baseline.
    private static TeamStats Healthy() => new()
    {
        Points = 80,
        FieldGoalsMade = 30,
        FieldGoalsAttempted = 60,
        ThreePointsMade = 7,
        ThreePointsAttempted = 20,
        FreeThrowsMade = 12,
        FreeThrowsAttempted = 16,
        OffensiveRebounds = 10,
        DefensiveRebounds = 30,
        Assists = 18,
        Turnovers = 12,
        Steals = 6,
        Blocks = 3,
        PersonalFouls = 15
    };

    // FG 27/60 = 0.45, 3P 6/20 = 0.30.
    private static TeamStats HealthyOpponent() => new()
    {
        Points = 78,
        FieldGoalsMade = 27,
        FieldGoalsAttempted = 60,
        ThreePointsMade = 6,
        ThreePointsAttempted = 20,
        FreeThrowsMade = 12,
        FreeThrowsAttempted = 15,
        OffensiveRebounds = 8,
        DefensiveRebounds = 28,
        Assists = 16,
        Turnovers = 14,
        Steals = 5,
        Blocks = 2,
        PersonalFouls = 16
    };

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
        var team = Healthy() with { Turnovers = opponent.Turnovers + diff };

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.TurnoverProblem));
    }

    [Fact]
    public void Evaluate_BadThreePointShootingWithVolume_TriggersOurShootingInefficiency()
    {
        // 3P 4/16 = 0.25 (<= OurBadThreePct 0.30), attempts 16 >= OurBadThreeAttemptsMin (15).
        var team = Healthy() with { ThreePointsMade = 4, ThreePointsAttempted = 16 };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.OurShootingInefficiency, tags);
    }

    [Fact]
    public void Evaluate_TooManyLowPercentageThreeAttempts_TriggersTooManyThreePointAttempts()
    {
        // 3P 12/40 = 0.30 (<= TooManyThreePctMax 0.33), attempts 40 >= TooManyThreeAttemptsMin (30).
        var team = Healthy() with { ThreePointsMade = 12, ThreePointsAttempted = 40 };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.TooManyThreePointAttempts, tags);
    }

    [Fact]
    public void Evaluate_LosingByEnoughWithLowFieldGoalPct_TriggersOffensiveEfficiencyProblem()
    {
        // Team FG 20/50 = 0.40 (<= OurLowFieldGoalPctForOffensiveEfficiency 0.45).
        var team = Healthy() with { Points = 70, FieldGoalsMade = 20, FieldGoalsAttempted = 50 };
        var opponent = HealthyOpponent() with { Points = 82 }; // +12 margin

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.OffensiveEfficiencyProblem, tags);
    }

    [Fact]
    public void Evaluate_FarFewerFoulsWhileNotAheadOnScore_TriggersLackOfPaintPressure()
    {
        var team = Healthy() with { Points = 70, PersonalFouls = 10 };
        var opponent = HealthyOpponent() with { Points = 78, PersonalFouls = 16 }; // team.Fouls <= opp.Fouls - 5

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.LackOfPaintPressure, tags);
    }

    [Theory]
    [InlineData(51, false)] // FG 51/100 = 0.51, just below OpponentHighFieldGoalPct (0.52)
    [InlineData(52, true)]  // FG 52/100 = 0.52, at threshold (>=)
    [InlineData(55, true)]  // FG 55/100 = 0.55, above threshold
    public void Evaluate_OpponentFieldGoalPct_Boundary(int made, bool expectTag)
    {
        var team = Healthy();
        var opponent = HealthyOpponent() with { FieldGoalsMade = made, FieldGoalsAttempted = 100 };

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.InteriorDefenseProblem));
    }

    [Fact]
    public void Evaluate_OpponentHotFromThreeWithVolume_TriggersOpponentHotFromThree()
    {
        // 3P 8/20 = 0.40 (>= OpponentHotThreePct 0.38), attempts 20 >= OpponentHotThreeAttemptsMin (20).
        var team = Healthy();
        var opponent = HealthyOpponent() with { ThreePointsMade = 8, ThreePointsAttempted = 20 };

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.OpponentHotFromThree, tags);
    }

    [Fact]
    public void Evaluate_OpponentAboveHardcodedThreePointRate_TriggersPerimeterDefenseProblem()
    {
        // PerimeterDefenseProblem bypasses RulesProfile: hardcoded opponent 3P% >= 0.36
        // and opponent 3PA >= team 3PA + 5.
        var team = Healthy() with { ThreePointsMade = 3, ThreePointsAttempted = 10 };
        // 3P 37/100 = 0.37.
        var opponent = HealthyOpponent() with { ThreePointsMade = 37, ThreePointsAttempted = 100 };

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.PerimeterDefenseProblem, tags);
    }

    [Fact]
    public void Evaluate_LosingByEnoughWithHighTurnovers_TriggersTransitionDefenseProblem()
    {
        var team = Healthy() with { Points = 65, Turnovers = 16 };
        var opponent = HealthyOpponent() with { Points = 80 }; // +15 margin

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.TransitionDefenseProblem, tags);
    }

    [Theory]
    [InlineData(4, false)] // team.PersonalFouls - opponent.PersonalFouls == FoulsDiffToFlag - 1
    [InlineData(5, true)]  // == FoulsDiffToFlag (boundary)
    [InlineData(6, true)]
    public void Evaluate_FoulsDiff_Boundary(int diff, bool expectTag)
    {
        var opponent = HealthyOpponent();
        var team = Healthy() with { PersonalFouls = opponent.PersonalFouls + diff };

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.FoulsProblem));
    }

    [Fact]
    public void Evaluate_OpponentOutreboundsOnOffensiveGlass_TriggersDefensiveReboundProblem()
    {
        var team = Healthy() with { OffensiveRebounds = 6 };
        var opponent = HealthyOpponent() with { OffensiveRebounds = 12 }; // diff = 6 >= 5

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
        var team = Healthy() with { Points = opponent.Points + margin };

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.PaceControlProblem));
    }

    [Fact]
    public void Evaluate_MultipleProblemsAtOnce_ReturnsAllTriggeredTagsWithoutDuplicates()
    {
        // Team commits far more turnovers AND has a big offensive-rebound deficit;
        // opponent also shoots well from the field. Three independent rules should fire.
        var team = Healthy() with { Points = 60, Turnovers = 22, OffensiveRebounds = 4 };
        // FG 55/100 = 0.55.
        var opponent = HealthyOpponent() with
        {
            Points = 70,
            FieldGoalsMade = 55,
            FieldGoalsAttempted = 100,
            OffensiveRebounds = 12
        };

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.TurnoverProblem, tags);
        Assert.Contains(ProblemTag.InteriorDefenseProblem, tags);
        Assert.Contains(ProblemTag.DefensiveReboundProblem, tags);
        Assert.Equal(tags.Distinct().Count(), tags.Count);
    }
}
