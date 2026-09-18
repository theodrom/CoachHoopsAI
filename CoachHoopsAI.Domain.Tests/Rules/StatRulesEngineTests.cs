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
        // FGA unchanged at Healthy()'s 60: rate = 40/60 = 0.667 (>= TooManyThreeAttemptRateMin
        // 0.40), pct = 12/40 = 0.30 (<= TooManyThreePctMax 0.33), FGA 60 >= TooManyThreeAttemptRateAttemptsMin (20).
        var team = Healthy() with { ThreePointsMade = 12, ThreePointsAttempted = 40 };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.TooManyThreePointAttempts, tags);
    }

    // Milestone 3: TooManyThreePointAttempts' volume gate is now a rate
    // (ThreePointAttemptRate = 3PA/FGA, M2A) instead of an absolute 3PA count - the
    // same raw count meant something different depending on total shot volume.
    // TooManyThreePctMax (the "shooting badly" gate) is unchanged and still
    // required, so a team taking a lot of threes AND making them is never flagged.
    // Default profile (Amateur-tier): TooManyThreeAttemptRateMin = 0.40,
    // TooManyThreeAttemptRateAttemptsMin = 20, TooManyThreePctMax = 0.33 (unchanged).

    [Theory]
    [InlineData(39, false)] // rate = 39/100 = 0.39, just below the 0.40 threshold
    [InlineData(40, true)]  // rate = 40/100 = 0.40, at threshold (>=, boundary)
    [InlineData(50, true)]  // rate = 50/100 = 0.50, above threshold
    public void Evaluate_ThreePointAttemptRate_Boundary(int threePointsAttempted, bool expectTag)
    {
        // 3PM tracks 30% of 3PA throughout, keeping pct = 0.30 (<= 0.33) constant so
        // only the rate varies across cases.
        var team = Healthy() with
        {
            FieldGoalsAttempted = 100,
            ThreePointsMade = (int)(threePointsAttempted * 0.3),
            ThreePointsAttempted = threePointsAttempted
        };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.TooManyThreePointAttempts));
    }

    [Theory]
    [InlineData(19, false)] // FieldGoalsAttempted just below TooManyThreeAttemptRateAttemptsMin (20)
    [InlineData(20, true)]  // at the minimum (boundary)
    [InlineData(25, true)]  // above the minimum
    public void Evaluate_ThreePointAttemptRate_InsufficientAttempts_DoesNotTrigger(int fieldGoalsAttempted, bool expectTag)
    {
        // 3PA is always 60% of FGA (rate = 0.60, comfortably above the 0.40
        // threshold) and 3PM is always 30% of 3PA (pct = 0.30, comfortably below
        // 0.33) at every case - only the sample size changes, proving the
        // minimum-attempts gate (not the rate or the percentage) is what suppresses
        // the tag below the cutoff. A few early shots in a live game can't trigger
        // this purely from a trivial sample.
        var threePointsAttempted = (int)(fieldGoalsAttempted * 0.6);
        var team = Healthy() with
        {
            FieldGoalsAttempted = fieldGoalsAttempted,
            ThreePointsMade = (int)(threePointsAttempted * 0.3),
            ThreePointsAttempted = threePointsAttempted
        };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.TooManyThreePointAttempts));
    }

    [Fact]
    public void Evaluate_ThreePointAttemptRate_IdenticalAttemptsDifferentTotalShotVolume_ProducesDifferentResult()
    {
        // Identical 3PA (30) and identical 3PM (9, pct = 0.30 <= 0.33 both times) -
        // only total FieldGoalsAttempted differs. This is exactly what the absolute-
        // count-based old trigger could not distinguish: 30 threes out of 60 total
        // shots (half the offense) is a very different signal from 30 out of 100
        // (less than a third), even though the raw 3PA count is the same.
        var smallShotDietTeam = Healthy() with { FieldGoalsAttempted = 60, ThreePointsMade = 9, ThreePointsAttempted = 30 };
        var largeShotDietTeam = Healthy() with { FieldGoalsAttempted = 100, ThreePointsMade = 9, ThreePointsAttempted = 30 };
        var opponent = HealthyOpponent();

        var smallDietMetrics = CalculatedMetricsCalculator.Calculate(smallShotDietTeam);
        var largeDietMetrics = CalculatedMetricsCalculator.Calculate(largeShotDietTeam);
        Assert.Equal(0.50, smallDietMetrics.ThreePointAttemptRate, precision: 10);
        Assert.Equal(0.30, largeDietMetrics.ThreePointAttemptRate, precision: 10);

        var smallDietTags = _engine.Evaluate(smallShotDietTeam, opponent, _profile);
        var largeDietTags = _engine.Evaluate(largeShotDietTeam, opponent, _profile);

        Assert.Contains(ProblemTag.TooManyThreePointAttempts, smallDietTags);
        Assert.DoesNotContain(ProblemTag.TooManyThreePointAttempts, largeDietTags);
    }

    [Fact]
    public void Evaluate_ThreePointAttemptRate_HighVolumeButGoodShooting_DoesNotTrigger()
    {
        // High rate (30/60 = 0.50, above the 0.40 threshold) but good shooting
        // (15/30 = 0.50, well above the 0.33 "bad shooting" cutoff) - proves the
        // label is never applied purely for volume. A team shooting well from three
        // is not an unsupported "too many" judgment, because the shooting-badly
        // gate still has to hold too.
        var team = Healthy() with { FieldGoalsAttempted = 60, ThreePointsMade = 15, ThreePointsAttempted = 30 };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.DoesNotContain(ProblemTag.TooManyThreePointAttempts, tags);
    }

    [Fact]
    public void Evaluate_ThreePointAttemptRate_ReadsThresholdsFromProfile_NotHardcoded()
    {
        // Identical team shooting (rate = 20/50 = 0.40, pct = 6/20 = 0.30) judged
        // against two profiles that differ only in TooManyThreeAttemptRateMin -
        // proves the volume threshold is profile-driven per level, not a hardcoded
        // constant.
        var team = Healthy() with { FieldGoalsAttempted = 50, ThreePointsMade = 6, ThreePointsAttempted = 20 };
        var opponent = HealthyOpponent();

        var lenientProfile = new RulesProfile { TooManyThreeAttemptRateMin = 0.45, TooManyThreeAttemptRateAttemptsMin = 10 };
        var strictProfile = new RulesProfile { TooManyThreeAttemptRateMin = 0.35, TooManyThreeAttemptRateAttemptsMin = 10 };

        var lenientTags = _engine.Evaluate(team, opponent, lenientProfile);
        var strictTags = _engine.Evaluate(team, opponent, strictProfile);

        Assert.DoesNotContain(ProblemTag.TooManyThreePointAttempts, lenientTags);
        Assert.Contains(ProblemTag.TooManyThreePointAttempts, strictTags);
    }

    // Milestone 3: OffensiveEfficiencyProblem's old trigger (raw FG% <= threshold,
    // gated on losing by a score margin) is replaced - being behind was never
    // necessary for an offense to be inefficient, and raw FG% ignores turnovers and
    // free throws entirely. It now reads OffensiveRating (points per 100 estimated
    // possessions, M2B) directly. Unlike LackOfPaintPressure, this reuses the same
    // ProblemTag rather than retiring it: the old trigger was a narrow, score-gated
    // proxy for the same underlying concept OffensiveRating measures directly, not
    // something conceptually unrelated. Default profile (Amateur-tier):
    // OurLowOffensiveRating = 95.0, OurLowOffensiveRatingPossessionsMin = 20.0.

    [Theory]
    [InlineData(96, false)] // OffensiveRating = 100*96/100 = 96, just above the 95 threshold
    [InlineData(95, true)]  // = 95, at threshold (<=, boundary)
    [InlineData(90, true)]  // = 90, below threshold
    public void Evaluate_OffensiveRating_Boundary(int points, bool expectTag)
    {
        // FGA=100, OREB=TO=FTA=0 -> EstimatedPossessions = 100 - 0 + 0 + 0.44*0 = 100
        // exactly, so OffensiveRating = 100 * Points / 100 = Points - a clean 1:1
        // mapping that isolates the threshold boundary.
        var team = Healthy() with
        {
            Points = points,
            FieldGoalsAttempted = 100,
            OffensiveRebounds = 0,
            Turnovers = 0,
            FreeThrowsMade = 0,
            FreeThrowsAttempted = 0
        };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.OffensiveEfficiencyProblem));
    }

    [Theory]
    [InlineData(19, false)] // EstimatedPossessions just below OurLowOffensiveRatingPossessionsMin (20)
    [InlineData(20, true)]  // at the minimum (boundary)
    [InlineData(25, true)]  // above the minimum
    public void Evaluate_OffensiveRating_InsufficientPossessions_DoesNotTrigger(int fieldGoalsAttempted, bool expectTag)
    {
        // Points fixed low (15) throughout, so OffensiveRating stays well below the
        // 95 threshold at every possession count below - only the sample size
        // changes, proving the minimum-possessions gate (not the rating itself) is
        // what suppresses the tag below the cutoff.
        var team = Healthy() with
        {
            Points = 15,
            FieldGoalsMade = (int)(fieldGoalsAttempted * 0.3),
            FieldGoalsAttempted = fieldGoalsAttempted,
            ThreePointsMade = 0,
            ThreePointsAttempted = 0,
            OffensiveRebounds = 0,
            Turnovers = 0,
            FreeThrowsMade = 0,
            FreeThrowsAttempted = 0
        };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Equal(expectTag, tags.Contains(ProblemTag.OffensiveEfficiencyProblem));
    }

    [Fact]
    public void Evaluate_OffensiveRating_ZeroEstimatedPossessions_RatingUnavailable_DoesNotTrigger()
    {
        // All-zero TeamStats (structurally valid, no invented negative stats) drives
        // EstimatedPossessions to exactly 0, so OffensiveRating is null
        // (GameCalculatedMetricsCalculator's zero-denominator convention). The gate
        // must not emit the finding when the rating itself is unavailable.
        var team = new TeamStats();
        var opponent = HealthyOpponent();

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent).Team;
        Assert.Null(metrics.OffensiveRating);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.DoesNotContain(ProblemTag.OffensiveEfficiencyProblem, tags);
    }

    [Fact]
    public void Evaluate_OffensiveRating_NegativeEstimatedPossessions_DoesNotTrigger()
    {
        // OffensiveRebounds (50) exceeds FieldGoalsAttempted + Turnovers + 0.44*FTA
        // (10), producing a negative EstimatedPossessions (-40). Whether this raw
        // input should be rejected upstream is a separate, out-of-scope validation
        // question (see CLAUDE.md's open questions) - but regardless of that policy,
        // this rule must never emit a finding from a non-positive possession
        // estimate. OffensiveRating is non-null here (RateOrNull only returns null
        // when the denominator is exactly zero), proving the possessions-min gate -
        // not a null check alone - is what protects this case.
        var team = new TeamStats { Points = 20, FieldGoalsAttempted = 10, OffensiveRebounds = 50 };
        var opponent = HealthyOpponent();

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent).Team;
        Assert.Equal(-40.0, metrics.EstimatedPossessions, precision: 10);
        Assert.NotNull(metrics.OffensiveRating);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.DoesNotContain(ProblemTag.OffensiveEfficiencyProblem, tags);
    }

    [Fact]
    public void Evaluate_OffensiveRating_LowWhileWinning_StillTriggers()
    {
        // The old trigger required losing by a margin; this proves that requirement
        // is gone - a team can have a low offensive rating, and get flagged for it,
        // even while leading on the scoreboard.
        var team = Healthy() with
        {
            Points = 50,
            FieldGoalsAttempted = 100,
            OffensiveRebounds = 0,
            Turnovers = 0,
            FreeThrowsMade = 0,
            FreeThrowsAttempted = 0
        }; // EstimatedPossessions = 100, OffensiveRating = 50 (well below the 95 threshold)
        var opponent = HealthyOpponent() with { Points = 40 };

        Assert.True(team.Points > opponent.Points); // sanity: team is ahead, not behind

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.OffensiveEfficiencyProblem, tags);
    }

    [Fact]
    public void Evaluate_OffensiveRating_CoOccursWithLowEffectiveFieldGoalPercentage_WithoutDuplicationOrInterference()
    {
        // Low eFG% and a low offensive rating can occur together - neither implies
        // the other, and both can legitimately fire on the same team/game without
        // one suppressing or duplicating the other.
        var team = Healthy() with
        {
            Points = 50,
            FieldGoalsMade = 16, // eFG% = 16/100 = 0.16, well below 0.47
            FieldGoalsAttempted = 100,
            ThreePointsMade = 0,
            ThreePointsAttempted = 0,
            OffensiveRebounds = 0,
            Turnovers = 0,
            FreeThrowsMade = 0,
            FreeThrowsAttempted = 0
        };
        var opponent = HealthyOpponent();

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.Contains(ProblemTag.LowEffectiveFieldGoalPercentage, tags);
        Assert.Contains(ProblemTag.OffensiveEfficiencyProblem, tags);
        Assert.Equal(tags.Distinct().Count(), tags.Count);
    }

    [Fact]
    public void Evaluate_OffensiveRating_LowDespiteGoodEffectiveFieldGoalPercentage_ProvesDistinctFromShootingFinding()
    {
        // Good shooting (eFG% = 25/50 = 0.50, above the 0.47 threshold) but a huge
        // turnover count inflates estimated possessions far beyond made shots,
        // driving points-per-possession down - proof that a low OffensiveRating
        // neither requires nor is implied by poor shooting efficiency; they are
        // distinct findings, not restatements of each other.
        var team = Healthy() with
        {
            Points = 50, // all 25 field goals were 2-pointers
            FieldGoalsMade = 25,
            FieldGoalsAttempted = 50,
            ThreePointsMade = 0,
            ThreePointsAttempted = 0,
            OffensiveRebounds = 0,
            Turnovers = 50,
            FreeThrowsMade = 0,
            FreeThrowsAttempted = 0
        };
        var opponent = HealthyOpponent();

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent).Team;
        Assert.Equal(0.50, metrics.EffectiveFieldGoalPercentage, precision: 10);
        Assert.Equal(100.0, metrics.EstimatedPossessions, precision: 10);
        Assert.Equal(50.0, metrics.OffensiveRating!.Value, precision: 10);

        var tags = _engine.Evaluate(team, opponent, _profile);

        Assert.DoesNotContain(ProblemTag.LowEffectiveFieldGoalPercentage, tags);
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
