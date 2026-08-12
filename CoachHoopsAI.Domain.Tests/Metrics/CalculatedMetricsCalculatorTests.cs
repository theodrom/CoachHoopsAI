using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Metrics;

namespace CoachHoopsAI.Domain.Tests.Metrics;

// Milestone 2A: proves the numerical primitive only - no thresholds, no judgments,
// no opponent/game-context dependence. See CLAUDE.md M2A scope.
public class CalculatedMetricsCalculatorTests
{
    private static TeamStats Representative() => new()
    {
        Points = 0, // irrelevant to these formulas
        FieldGoalsMade = 20,
        FieldGoalsAttempted = 40,
        ThreePointsMade = 6,
        ThreePointsAttempted = 15,
        FreeThrowsMade = 12,
        FreeThrowsAttempted = 16,
        OffensiveRebounds = 8,
        DefensiveRebounds = 21,
        Assists = 18,
        Turnovers = 9,
        Steals = 0,
        Blocks = 0,
        PersonalFouls = 0
    };

    [Fact]
    public void Calculate_RepresentativeStats_ProducesExpectedMetrics()
    {
        var metrics = CalculatedMetricsCalculator.Calculate(Representative());

        Assert.Equal(0.500, metrics.FieldGoalPercentage, precision: 10);
        Assert.Equal(0.400, metrics.ThreePointPercentage, precision: 10);
        Assert.Equal(0.750, metrics.FreeThrowPercentage, precision: 10);
        Assert.Equal(29, metrics.TotalRebounds);
        Assert.Equal(0.575, metrics.EffectiveFieldGoalPercentage, precision: 10);
        Assert.Equal(2.0, metrics.AssistToTurnoverRatio!.Value, precision: 10);
        Assert.Equal(0.375, metrics.ThreePointAttemptRate, precision: 10);
        Assert.Equal(0.400, metrics.FreeThrowRate, precision: 10);
    }

    [Fact]
    public void Calculate_AllZeroStats_ReturnsZeroesAndNullRatio()
    {
        var metrics = CalculatedMetricsCalculator.Calculate(new TeamStats());

        Assert.Equal(0.0, metrics.FieldGoalPercentage);
        Assert.Equal(0.0, metrics.ThreePointPercentage);
        Assert.Equal(0.0, metrics.FreeThrowPercentage);
        Assert.Equal(0, metrics.TotalRebounds);
        Assert.Equal(0.0, metrics.EffectiveFieldGoalPercentage);
        Assert.Null(metrics.AssistToTurnoverRatio);
        Assert.Equal(0.0, metrics.ThreePointAttemptRate);
        Assert.Equal(0.0, metrics.FreeThrowRate);
    }

    [Fact]
    public void Calculate_FieldGoalsAttemptedZero_ReturnsZeroForAllFgaDependentMetrics()
    {
        var stats = new TeamStats
        {
            FieldGoalsMade = 0,
            FieldGoalsAttempted = 0,
            ThreePointsMade = 0,
            ThreePointsAttempted = 5, // unreachable in practice, but proves FGA drives these, not 3PA
            FreeThrowsMade = 4,
            FreeThrowsAttempted = 5
        };

        var metrics = CalculatedMetricsCalculator.Calculate(stats);

        Assert.Equal(0.0, metrics.FieldGoalPercentage);
        Assert.Equal(0.0, metrics.EffectiveFieldGoalPercentage);
        Assert.Equal(0.0, metrics.ThreePointAttemptRate);
        Assert.Equal(0.0, metrics.FreeThrowRate);
    }

    [Fact]
    public void Calculate_ThreePointsAttemptedZero_ReturnsZeroThreePointPercentage()
    {
        var stats = new TeamStats { ThreePointsMade = 0, ThreePointsAttempted = 0 };

        var metrics = CalculatedMetricsCalculator.Calculate(stats);

        Assert.Equal(0.0, metrics.ThreePointPercentage);
    }

    [Fact]
    public void Calculate_FreeThrowsAttemptedZero_ReturnsZeroFreeThrowPercentage()
    {
        var stats = new TeamStats { FreeThrowsMade = 0, FreeThrowsAttempted = 0 };

        var metrics = CalculatedMetricsCalculator.Calculate(stats);

        Assert.Equal(0.0, metrics.FreeThrowPercentage);
    }

    [Fact]
    public void Calculate_TurnoversZeroWithAssists_ReturnsNullRatio_NotZero()
    {
        var stats = new TeamStats { Assists = 12, Turnovers = 0 };

        var metrics = CalculatedMetricsCalculator.Calculate(stats);

        Assert.Null(metrics.AssistToTurnoverRatio);
    }

    [Fact]
    public void Calculate_AssistsAndTurnoversBothZero_ReturnsNullRatio()
    {
        var stats = new TeamStats { Assists = 0, Turnovers = 0 };

        var metrics = CalculatedMetricsCalculator.Calculate(stats);

        Assert.Null(metrics.AssistToTurnoverRatio);
    }

    [Fact]
    public void Calculate_FreeThrowRate_CanExceedOne()
    {
        var stats = new TeamStats { FieldGoalsAttempted = 10, FreeThrowsAttempted = 15 };

        var metrics = CalculatedMetricsCalculator.Calculate(stats);

        Assert.Equal(1.5, metrics.FreeThrowRate, precision: 10);
    }

    [Fact]
    public void Calculate_EffectiveFieldGoalPercentage_IsNotClamped_CanExceedOne()
    {
        // All makes are threes: (10 + 0.5 * 10) / 10 = 1.5, above the 1.0 a naive cap would enforce.
        var stats = new TeamStats
        {
            FieldGoalsMade = 10,
            FieldGoalsAttempted = 10,
            ThreePointsMade = 10,
            ThreePointsAttempted = 10
        };

        var metrics = CalculatedMetricsCalculator.Calculate(stats);

        Assert.Equal(1.5, metrics.EffectiveFieldGoalPercentage, precision: 10);
    }
}
