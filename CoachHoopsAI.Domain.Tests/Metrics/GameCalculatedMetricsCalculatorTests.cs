using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.GameContext;
using CoachHoopsAI.Domain.Metrics;

namespace CoachHoopsAI.Domain.Tests.Metrics;

// Milestone 2B: possession-dependent and opponent-dependent metrics, calculated
// symmetrically for Team and Opponent. Depends on CalculatedMetricsCalculator (M2A)
// for each side's single-team metrics; this file does not re-test M2A's own
// zero-denominator edge cases (CalculatedMetricsCalculatorTests already covers
// those) - only that the game calculator carries them through unchanged.
//
// Milestone 2C (below): Estimated Pace, via the GameFormat/GameTiming-aware overload.
public class GameCalculatedMetricsCalculatorTests
{
    // FGA=70, OREB=10, TO=15, FTA=20 -> Possessions = 70 - 10 + 15 + 0.44*20 = 83.8
    private static TeamStats Team() => new()
    {
        Points = 85,
        FieldGoalsMade = 30,
        FieldGoalsAttempted = 70,
        ThreePointsMade = 8,
        ThreePointsAttempted = 22,
        FreeThrowsMade = 15,
        FreeThrowsAttempted = 20,
        OffensiveRebounds = 10,
        DefensiveRebounds = 30,
        Assists = 20,
        Turnovers = 15,
        Steals = 9,
        Blocks = 4,
        PersonalFouls = 17
    };

    // FGA=65, OREB=12, TO=13, FTA=18 -> Possessions = 65 - 12 + 13 + 0.44*18 = 73.92
    private static TeamStats Opponent() => new()
    {
        Points = 80,
        FieldGoalsMade = 28,
        FieldGoalsAttempted = 65,
        ThreePointsMade = 7,
        ThreePointsAttempted = 19,
        FreeThrowsMade = 14,
        FreeThrowsAttempted = 18,
        OffensiveRebounds = 12,
        DefensiveRebounds = 28,
        Assists = 18,
        Turnovers = 13,
        Steals = 7,
        Blocks = 3,
        PersonalFouls = 19
    };

    [Fact]
    public void Calculate_RepresentativeStats_ProducesExpectedPossessionAndOpponentDependentMetrics()
    {
        var team = Team();
        var opponent = Opponent();

        // Same formulas the calculator uses, spelled out here so the basketball
        // math is auditable against the milestone spec rather than hand-typed
        // decimal literals.
        var teamPossessions = team.FieldGoalsAttempted - team.OffensiveRebounds + team.Turnovers + 0.44 * team.FreeThrowsAttempted;
        var opponentPossessions = opponent.FieldGoalsAttempted - opponent.OffensiveRebounds + opponent.Turnovers + 0.44 * opponent.FreeThrowsAttempted;

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent);

        Assert.Equal(83.8, teamPossessions, precision: 10);
        Assert.Equal(73.92, opponentPossessions, precision: 10);
        Assert.Equal(teamPossessions, metrics.Team.EstimatedPossessions, precision: 10);
        Assert.Equal(opponentPossessions, metrics.Opponent.EstimatedPossessions, precision: 10);

        Assert.Equal(100.0 * team.Points / teamPossessions, metrics.Team.OffensiveRating!.Value, precision: 10);
        Assert.Equal(100.0 * opponent.Points / opponentPossessions, metrics.Opponent.OffensiveRating!.Value, precision: 10);

        // 15 / 83.8 and 13 / 73.92 are both non-terminating - proves no internal rounding.
        Assert.Equal(team.Turnovers / teamPossessions, metrics.Team.TurnoverRate!.Value, precision: 10);
        Assert.Equal(opponent.Turnovers / opponentPossessions, metrics.Opponent.TurnoverRate!.Value, precision: 10);

        // Team OREB% = 10 / (10 + 28) = 10/38; Opponent OREB% = 12 / (12 + 30) = 12/42.
        Assert.Equal((double)team.OffensiveRebounds / (team.OffensiveRebounds + opponent.DefensiveRebounds), metrics.Team.OffensiveReboundPercentage!.Value, precision: 10);
        Assert.Equal((double)opponent.OffensiveRebounds / (opponent.OffensiveRebounds + team.DefensiveRebounds), metrics.Opponent.OffensiveReboundPercentage!.Value, precision: 10);

        // Team DREB% = 30 / (30 + 12) = 30/42; Opponent DREB% = 28 / (28 + 10) = 28/38.
        Assert.Equal((double)team.DefensiveRebounds / (team.DefensiveRebounds + opponent.OffensiveRebounds), metrics.Team.DefensiveReboundPercentage!.Value, precision: 10);
        Assert.Equal((double)opponent.DefensiveRebounds / (opponent.DefensiveRebounds + team.OffensiveRebounds), metrics.Opponent.DefensiveReboundPercentage!.Value, precision: 10);

        Assert.Equal(team.Steals / opponentPossessions, metrics.Team.StealRate!.Value, precision: 10);
        Assert.Equal(opponent.Steals / teamPossessions, metrics.Opponent.StealRate!.Value, precision: 10);

        Assert.Equal(team.PersonalFouls / opponentPossessions, metrics.Team.FoulRate!.Value, precision: 10);
        Assert.Equal(opponent.PersonalFouls / teamPossessions, metrics.Opponent.FoulRate!.Value, precision: 10);
    }

    [Fact]
    public void Calculate_TeamOrebPlusOpponentDrebIsZero_ReturnsNullForBothSidesOfThatPair()
    {
        var team = Team() with { OffensiveRebounds = 0 };
        var opponent = Opponent() with { DefensiveRebounds = 0 };

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent);

        // Team OREB% and Opponent DREB% share the same (TeamOREB + OpponentDREB) denominator.
        Assert.Null(metrics.Team.OffensiveReboundPercentage);
        Assert.Null(metrics.Opponent.DefensiveReboundPercentage);
    }

    [Fact]
    public void Calculate_TeamDrebPlusOpponentOrebIsZero_ReturnsNullForBothSidesOfThatPair()
    {
        var team = Team() with { DefensiveRebounds = 0 };
        var opponent = Opponent() with { OffensiveRebounds = 0 };

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent);

        // Team DREB% and Opponent OREB% share the same (TeamDREB + OpponentOREB) denominator.
        Assert.Null(metrics.Team.DefensiveReboundPercentage);
        Assert.Null(metrics.Opponent.OffensiveReboundPercentage);
    }

    [Fact]
    public void Calculate_TeamEstimatedPossessionsZero_TeamRatingAndTurnoverRateAreNull_AndOpponentRatesDependingOnItAreNull()
    {
        // All-zero TeamStats is structurally valid (no negative counts) and drives
        // EstimatedPossessions = 0 - 0 + 0 + 0.44*0 = 0 without inventing invalid stats.
        var team = new TeamStats();
        var opponent = Opponent();

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent);

        Assert.Equal(0.0, metrics.Team.EstimatedPossessions);
        Assert.Null(metrics.Team.OffensiveRating);
        Assert.Null(metrics.Team.TurnoverRate);

        // Opponent StealRate/FoulRate divide by Team's (zero) possessions.
        Assert.Null(metrics.Opponent.StealRate);
        Assert.Null(metrics.Opponent.FoulRate);
    }

    [Fact]
    public void Calculate_EstimatedPossessions_UsesPoint44TimesFreeThrowsAttemptedExactly()
    {
        var team = new TeamStats
        {
            FieldGoalsAttempted = 50,
            OffensiveRebounds = 5,
            Turnovers = 10,
            FreeThrowsAttempted = 25 // 0.44 * 25 = 11.0 exactly
        };
        var opponent = new TeamStats();

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent);

        Assert.Equal(66.0, metrics.Team.EstimatedPossessions, precision: 10);
    }

    [Fact]
    public void Calculate_TeamAndOpponentSwapped_SwapsCorrespondingOutputs()
    {
        var team = Team();
        var opponent = Opponent();

        var straight = GameCalculatedMetricsCalculator.Calculate(team, opponent);
        var swapped = GameCalculatedMetricsCalculator.Calculate(opponent, team);

        Assert.Equal(straight.Team, swapped.Opponent);
        Assert.Equal(straight.Opponent, swapped.Team);
    }

    [Fact]
    public void Calculate_M2ASingleTeamMetrics_MatchCalculatedMetricsCalculatorDirectly()
    {
        var team = Team();
        var opponent = Opponent();

        var expectedTeamM2A = CalculatedMetricsCalculator.Calculate(team);
        var expectedOpponentM2A = CalculatedMetricsCalculator.Calculate(opponent);

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent);

        Assert.Equal(expectedTeamM2A.FieldGoalPercentage, metrics.Team.FieldGoalPercentage);
        Assert.Equal(expectedTeamM2A.ThreePointPercentage, metrics.Team.ThreePointPercentage);
        Assert.Equal(expectedTeamM2A.FreeThrowPercentage, metrics.Team.FreeThrowPercentage);
        Assert.Equal(expectedTeamM2A.TotalRebounds, metrics.Team.TotalRebounds);
        Assert.Equal(expectedTeamM2A.EffectiveFieldGoalPercentage, metrics.Team.EffectiveFieldGoalPercentage);
        Assert.Equal(expectedTeamM2A.AssistToTurnoverRatio, metrics.Team.AssistToTurnoverRatio);
        Assert.Equal(expectedTeamM2A.ThreePointAttemptRate, metrics.Team.ThreePointAttemptRate);
        Assert.Equal(expectedTeamM2A.FreeThrowRate, metrics.Team.FreeThrowRate);

        Assert.Equal(expectedOpponentM2A.FieldGoalPercentage, metrics.Opponent.FieldGoalPercentage);
        Assert.Equal(expectedOpponentM2A.ThreePointPercentage, metrics.Opponent.ThreePointPercentage);
        Assert.Equal(expectedOpponentM2A.FreeThrowPercentage, metrics.Opponent.FreeThrowPercentage);
        Assert.Equal(expectedOpponentM2A.TotalRebounds, metrics.Opponent.TotalRebounds);
        Assert.Equal(expectedOpponentM2A.EffectiveFieldGoalPercentage, metrics.Opponent.EffectiveFieldGoalPercentage);
        Assert.Equal(expectedOpponentM2A.AssistToTurnoverRatio, metrics.Opponent.AssistToTurnoverRatio);
        Assert.Equal(expectedOpponentM2A.ThreePointAttemptRate, metrics.Opponent.ThreePointAttemptRate);
        Assert.Equal(expectedOpponentM2A.FreeThrowRate, metrics.Opponent.FreeThrowRate);
    }

    private static readonly GameFormat FourByTen = new()
    {
        RegulationPeriods = 4,
        RegulationPeriodMinutes = 10,
        OvertimePeriodMinutes = 5
    };

    private static readonly GameFormat FourByTwelve = new()
    {
        RegulationPeriods = 4,
        RegulationPeriodMinutes = 12,
        OvertimePeriodMinutes = 5
    };

    // EasyBasket-style, non-NBA/FIBA shape - proves the calculation reads GameFormat
    // generically rather than assuming 40 or 48 regulation minutes.
    private static readonly GameFormat EightByFour = new()
    {
        RegulationPeriods = 8,
        RegulationPeriodMinutes = 4,
        OvertimePeriodMinutes = 4
    };

    private static double ExpectedGameEstimatedPossessions(TeamStats team, TeamStats opponent)
    {
        var teamPossessions = team.FieldGoalsAttempted - team.OffensiveRebounds + team.Turnovers + 0.44 * team.FreeThrowsAttempted;
        var opponentPossessions = opponent.FieldGoalsAttempted - opponent.OffensiveRebounds + opponent.Turnovers + 0.44 * opponent.FreeThrowsAttempted;
        return (teamPossessions + opponentPossessions) / 2.0;
    }

    [Fact]
    public void Calculate_Pace_Representative4x10_MatchesFormula()
    {
        var team = Team();
        var opponent = Opponent();
        // Period 3, 4:00 remaining of a 10-minute period -> 2 completed periods (20)
        // + (10 - 4) elapsed in period 3 = 26 minutes elapsed.
        var timing = new GameTiming { CurrentPeriod = 3, ClockRemaining = TimeSpan.FromMinutes(4) };

        var expectedGamePossessions = ExpectedGameEstimatedPossessions(team, opponent);
        var expectedElapsedMinutes = timing.ElapsedGameTime(FourByTen).TotalMinutes;
        // GameEstimatedPossessions x RegulationDurationMinutes / ElapsedGameTimeMinutes.
        // 26 does not divide evenly, so this is also a no-internal-rounding case.
        var expectedPace = expectedGamePossessions * FourByTen.RegulationDuration.TotalMinutes / expectedElapsedMinutes;

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent, FourByTen, timing);

        Assert.Equal(26.0, expectedElapsedMinutes, precision: 10);
        Assert.Equal(expectedGamePossessions, metrics.GameEstimatedPossessions, precision: 10);
        Assert.Equal(expectedPace, metrics.EstimatedPace!.Value, precision: 10);
    }

    [Fact]
    public void Calculate_Pace_4x12Format_NormalizesAgainst48RegulationMinutes_NotHardcoded40()
    {
        var team = Team();
        var opponent = Opponent();
        // Period 2, 5:00 remaining of a 12-minute period -> 1 completed period (12)
        // + (12 - 5) elapsed in period 2 = 19 minutes elapsed.
        var timing = new GameTiming { CurrentPeriod = 2, ClockRemaining = TimeSpan.FromMinutes(5) };

        var expectedGamePossessions = ExpectedGameEstimatedPossessions(team, opponent);
        var expectedElapsedMinutes = timing.ElapsedGameTime(FourByTwelve).TotalMinutes;
        var expectedPace = expectedGamePossessions * FourByTwelve.RegulationDuration.TotalMinutes / expectedElapsedMinutes;

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent, FourByTwelve, timing);

        Assert.Equal(48.0, FourByTwelve.RegulationDuration.TotalMinutes);
        Assert.Equal(19.0, expectedElapsedMinutes, precision: 10);
        Assert.Equal(expectedPace, metrics.EstimatedPace!.Value, precision: 10);
    }

    [Fact]
    public void Calculate_Pace_NonStandard8x4Format_UsesGameFormatGenerically()
    {
        var team = Team();
        var opponent = Opponent();
        // Period 6, 3:00 remaining of a 4-minute period -> 5 completed periods (20)
        // + (4 - 3) elapsed in period 6 = 21 minutes elapsed. Matches
        // GameTimingTests.EasyBasketStyleFormat_8x4_ElapsedGameTimeIsCorrect.
        var timing = new GameTiming { CurrentPeriod = 6, ClockRemaining = TimeSpan.FromMinutes(3) };

        var expectedGamePossessions = ExpectedGameEstimatedPossessions(team, opponent);
        var expectedElapsedMinutes = timing.ElapsedGameTime(EightByFour).TotalMinutes;
        var expectedPace = expectedGamePossessions * EightByFour.RegulationDuration.TotalMinutes / expectedElapsedMinutes;

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent, EightByFour, timing);

        Assert.Equal(32.0, EightByFour.RegulationDuration.TotalMinutes);
        Assert.Equal(21.0, expectedElapsedMinutes, precision: 10);
        Assert.Equal(expectedPace, metrics.EstimatedPace!.Value, precision: 10);
    }

    [Fact]
    public void Calculate_Pace_ExactGameStart_ReturnsNull_ButGameEstimatedPossessionsStillReflectsStats()
    {
        var team = Team();
        var opponent = Opponent();
        var timing = new GameTiming { CurrentPeriod = 1, ClockRemaining = TimeSpan.FromMinutes(FourByTen.RegulationPeriodMinutes) };

        Assert.Equal(TimeSpan.Zero, timing.ElapsedGameTime(FourByTen));

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent, FourByTen, timing);

        Assert.Null(metrics.EstimatedPace);
        Assert.Equal(ExpectedGameEstimatedPossessions(team, opponent), metrics.GameEstimatedPossessions, precision: 10);
    }

    [Fact]
    public void Calculate_Pace_Overtime_ElapsedIncludesOT_ButNormalizationStaysAtRegulationDuration()
    {
        var team = Team();
        var opponent = Opponent();
        // Period 5 = OT1 for a 4-period format. 2:00 remaining of a 5-minute OT period
        // -> ElapsedGameTime = 40 (regulation) + (5 - 2) = 43 minutes.
        var timing = new GameTiming { CurrentPeriod = 5, ClockRemaining = TimeSpan.FromMinutes(2) };

        Assert.True(timing.IsOvertime(FourByTen));
        var expectedElapsedMinutes = timing.ElapsedGameTime(FourByTen).TotalMinutes;
        Assert.Equal(43.0, expectedElapsedMinutes, precision: 10);

        var expectedGamePossessions = ExpectedGameEstimatedPossessions(team, opponent);
        // Normalizes against the 40-minute regulation duration, not 45 (regulation + OT1).
        var expectedPace = expectedGamePossessions * FourByTen.RegulationDuration.TotalMinutes / expectedElapsedMinutes;

        var metrics = GameCalculatedMetricsCalculator.Calculate(team, opponent, FourByTen, timing);

        Assert.Equal(expectedPace, metrics.EstimatedPace!.Value, precision: 10);
    }

    [Fact]
    public void Calculate_Pace_TeamAndOpponentSwapped_GameLevelValuesUnchanged()
    {
        var team = Team();
        var opponent = Opponent();
        var timing = new GameTiming { CurrentPeriod = 3, ClockRemaining = TimeSpan.FromMinutes(4) };

        var straight = GameCalculatedMetricsCalculator.Calculate(team, opponent, FourByTen, timing);
        var swapped = GameCalculatedMetricsCalculator.Calculate(opponent, team, FourByTen, timing);

        Assert.Equal(straight.GameEstimatedPossessions, swapped.GameEstimatedPossessions);
        Assert.Equal(straight.EstimatedPace!.Value, swapped.EstimatedPace!.Value, precision: 10);
    }

    [Fact]
    public void Calculate_PaceAwareOverload_PreservesM2AAndM2BMetrics_FromTwoArgOverload()
    {
        var team = Team();
        var opponent = Opponent();
        var timing = new GameTiming { CurrentPeriod = 3, ClockRemaining = TimeSpan.FromMinutes(4) };

        var withoutTiming = GameCalculatedMetricsCalculator.Calculate(team, opponent);
        var withTiming = GameCalculatedMetricsCalculator.Calculate(team, opponent, FourByTen, timing);

        Assert.Null(withoutTiming.EstimatedPace);
        Assert.Equal(withoutTiming.Team, withTiming.Team);
        Assert.Equal(withoutTiming.Opponent, withTiming.Opponent);
        Assert.Equal(withoutTiming.GameEstimatedPossessions, withTiming.GameEstimatedPossessions, precision: 10);
    }
}
