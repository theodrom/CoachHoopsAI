using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Metrics;
using CoachHoopsAI.Domain.Rules;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Domain.Tests.Rules;

// Locks down today's StatRulesEngine behavior (13 threshold-based rules against a
// RulesProfile) so it can be safely evolved in a later milestone. TeamStats now
// stores only raw made/attempted counts (Milestone 1); every percentage-driven
// scenario below is built from an exact made/attempted ratio rather than a stored
// percentage, per the engine's LegacyPercentageBridge (FGM/FGA, 3PM/3PA) for the
// legacy rules, or CalculatedMetricsCalculator (M2A) for LowEffectiveFieldGoalPercentage.
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

    // Milestone 3: LowEffectiveFieldGoalPercentage reads TeamCalculatedMetrics'
    // EffectiveFieldGoalPercentage (M2A) directly, not LegacyPercentageBridge.
    // Default profile (Amateur-tier): OurLowEffectiveFieldGoalPct = 0.47,
    // OurLowEffectiveFieldGoalPctAttemptsMin = 20.

    [Theory]
    [InlineData(48, false)] // eFG% 48/100 = 0.48, just above the 0.47 threshold
    [InlineData(47, true)]  // eFG% 47/100 = 0.47, at threshold (<=, boundary)
    [InlineData(44, true)]  // eFG% 44/100 = 0.44, below threshold
    public void Evaluate_EffectiveFieldGoalPct_Boundary(int fieldGoalsMade, bool expectTag)
    {
        // 3PM = 0 keeps eFG% numerically equal to FG% here, isolating the threshold
        // boundary from the three-point weighting (covered separately below).
        var team = Healthy() with
        {
            FieldGoalsMade = fieldGoalsMade,
            FieldGoalsAttempted = 100,
            ThreePointsMade = 0,
            ThreePointsAttempted = 0
        };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.LowEffectiveFieldGoalPercentage));
    }

    [Theory]
    [InlineData(19, false)] // FieldGoalsAttempted just below OurLowEffectiveFieldGoalPctAttemptsMin (20)
    [InlineData(20, true)]  // at the minimum (boundary)
    [InlineData(25, true)]  // above the minimum
    public void Evaluate_EffectiveFieldGoalPct_InsufficientAttempts_DoesNotTrigger(int fieldGoalsAttempted, bool expectTag)
    {
        // eFG% stays clearly below the 0.47 threshold at every attempt count below
        // (~0.37-0.40) - only the sample size changes, proving the minimum-attempts
        // gate (not the percentage itself) is what suppresses the tag below the cutoff.
        var fieldGoalsMade = (int)(fieldGoalsAttempted * 0.4);
        var team = Healthy() with
        {
            FieldGoalsMade = fieldGoalsMade,
            FieldGoalsAttempted = fieldGoalsAttempted,
            ThreePointsMade = 0,
            ThreePointsAttempted = 0
        };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.LowEffectiveFieldGoalPercentage));
    }

    [Fact]
    public void Evaluate_EffectiveFieldGoalPct_CreditsThreePointers_SoRawFieldGoalPctAloneWouldMisjudgeIt()
    {
        // FG 40/100 = 0.40 (would itself be well below 0.47 if judged on raw FG%),
        // but half those makes are threes: eFG% = (40 + 0.5*20) / 100 = 0.50, above
        // the threshold. The rule must read eFG%, not raw FG%, to avoid flagging an
        // efficient three-point shooting team as inefficient.
        var team = Healthy() with
        {
            FieldGoalsMade = 40,
            FieldGoalsAttempted = 100,
            ThreePointsMade = 20,
            ThreePointsAttempted = 45
        };
        var opponent = HealthyOpponent();

        var metrics = CalculatedMetricsCalculator.Calculate(team);
        Assert.Equal(0.40, metrics.FieldGoalPercentage, precision: 10);
        Assert.Equal(0.50, metrics.EffectiveFieldGoalPercentage, precision: 10);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.DoesNotContain(ProblemTag.LowEffectiveFieldGoalPercentage, tags);
    }

    [Fact]
    public void Evaluate_EffectiveFieldGoalPct_ReadsThresholdsFromProfile_NotHardcoded()
    {
        // Identical team shooting (eFG% = 18/40 = 0.45) judged against two
        // different level-style profiles: lenient (EasyBasket_Default-like) vs.
        // strict (Pro_Default-like). Proves the rule is profile-driven per level,
        // not a hardcoded constant (unlike PerimeterDefenseProblem elsewhere in
        // this engine).
        var team = Healthy() with
        {
            FieldGoalsMade = 18,
            FieldGoalsAttempted = 40,
            ThreePointsMade = 0,
            ThreePointsAttempted = 0
        };
        var opponent = HealthyOpponent();

        var lenientProfile = new RulesProfile { OurLowEffectiveFieldGoalPct = 0.42, OurLowEffectiveFieldGoalPctAttemptsMin = 10 };
        var strictProfile = new RulesProfile { OurLowEffectiveFieldGoalPct = 0.48, OurLowEffectiveFieldGoalPctAttemptsMin = 25 };

        var lenientTags = _engine.Evaluate(team, opponent, lenientProfile);
        var strictTags = _engine.Evaluate(team, opponent, strictProfile);

        Assert.DoesNotContain(ProblemTag.LowEffectiveFieldGoalPercentage, lenientTags);
        Assert.Contains(ProblemTag.LowEffectiveFieldGoalPercentage, strictTags);
    }

    [Fact]
    public void Evaluate_EffectiveFieldGoalPct_CoOccursWithUnrelatedTags_WithoutDuplicationOrInterference()
    {
        // Low eFG% (well under 20 attempts minimum, so it fires) alongside a
        // separately-triggered TurnoverProblem; FoulsProblem stays absent since
        // nothing here drives a foul differential. Confirms the new rule composes
        // cleanly with pre-existing, unrelated findings.
        var opponent = HealthyOpponent();
        var team = Healthy() with
        {
            FieldGoalsMade = 16,
            FieldGoalsAttempted = 40,
            ThreePointsMade = 0,
            ThreePointsAttempted = 0,
            Turnovers = opponent.Turnovers + 5
        };

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.LowEffectiveFieldGoalPercentage, tags);
        Assert.Contains(ProblemTag.TurnoverProblem, tags);
        Assert.DoesNotContain(ProblemTag.FoulsProblem, tags);
        Assert.Equal(tags.Distinct().Count(), tags.Count);
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

    // Milestone 3: LackOfPaintPressure's old trigger (personal fouls far below the
    // opponent's, while not leading on score) is retired - it had no defensible
    // connection to paint/rim pressure, and TeamStats has no shot-location data to
    // support a real "paint scoring" finding. LowFreeThrowRate (FTA/FGA, M2A)
    // replaces it as a purely literal finding - few free-throw attempts relative to
    // field-goal attempts - and does not claim or imply interior/rim aggression:
    // free throws arise from several situations besides paint drives, and real
    // paint attacks often draw no whistle at all. Default profile (Amateur-tier):
    // OurLowFreeThrowRate = 0.18, OurLowFreeThrowRateAttemptsMin = 20.

    [Fact]
    public void Evaluate_FoulsAndScoreAlone_NoLongerTriggerLackOfPaintPressure()
    {
        // The exact scenario that used to trigger the retired rule: team fouls far
        // below the opponent's (<= opponent - 5) while not leading on score. Neither
        // signal is read by StatRulesEngine anymore for this finding.
        var team = Healthy() with { Points = 70, PersonalFouls = 10 };
        var opponent = HealthyOpponent() with { Points = 78, PersonalFouls = 16 }; // team.Fouls <= opp.Fouls - 5

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.DoesNotContain(ProblemTag.LackOfPaintPressure, tags);
    }

    [Theory]
    [InlineData(19, false)] // FT rate 19/100 = 0.19, just above the 0.18 threshold
    [InlineData(18, true)]  // FT rate 18/100 = 0.18, at threshold (<=, boundary)
    [InlineData(15, true)]  // FT rate 15/100 = 0.15, below threshold
    public void Evaluate_FreeThrowRate_Boundary(int freeThrowsAttempted, bool expectTag)
    {
        var team = Healthy() with { FieldGoalsAttempted = 100, FreeThrowsAttempted = freeThrowsAttempted };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.LowFreeThrowRate));
    }

    [Theory]
    [InlineData(19, false)] // FieldGoalsAttempted just below OurLowFreeThrowRateAttemptsMin (20)
    [InlineData(20, true)]  // at the minimum (boundary)
    [InlineData(25, true)]  // above the minimum
    public void Evaluate_FreeThrowRate_InsufficientAttempts_DoesNotTrigger(int fieldGoalsAttempted, bool expectTag)
    {
        // FreeThrowsMade/Attempted stay at 1/2 throughout, so the rate falls well
        // below 0.18 at every field-goal-attempt count below - only the sample size
        // changes, proving the minimum-attempts gate (not the rate itself) is what
        // suppresses the tag below the cutoff.
        var team = Healthy() with { FieldGoalsAttempted = fieldGoalsAttempted, FreeThrowsMade = 1, FreeThrowsAttempted = 2 };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.LowFreeThrowRate));
    }

    [Fact]
    public void Evaluate_FreeThrowRate_ZeroFieldGoalAttempts_DoesNotTrigger()
    {
        // CalculatedMetricsCalculator returns FreeThrowRate == 0.0 when
        // FieldGoalsAttempted == 0 (M2A's zero-denominator convention), which would
        // otherwise misread as "maximally low" for a team that simply hasn't shot
        // yet (e.g. very early in a live game). The attempts gate must block this.
        var team = Healthy() with { FieldGoalsAttempted = 0, FreeThrowsMade = 0, FreeThrowsAttempted = 0 };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.DoesNotContain(ProblemTag.LowFreeThrowRate, tags);
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
