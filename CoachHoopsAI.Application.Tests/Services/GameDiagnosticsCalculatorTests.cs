using CoachHoopsAI.Application.Services;
using CoachHoopsAI.Domain.Entities;

namespace CoachHoopsAI.Application.Tests.Services;

public class GameDiagnosticsCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsTeamMinusOpponentDiffsAndPassesThroughAppliedProfile()
    {
        // Team FG 30/60 = 0.50, 3P 8/20 = 0.40.
        var team = new TeamStats
        {
            Points = 80,
            FieldGoalsMade = 30,
            FieldGoalsAttempted = 60,
            ThreePointsMade = 8,
            ThreePointsAttempted = 20,
            OffensiveRebounds = 10,
            DefensiveRebounds = 30,
            Turnovers = 12,
            PersonalFouls = 15
        };
        // Opponent FG 22/50 = 0.44, 3P 6/20 = 0.30.
        var opponent = new TeamStats
        {
            Points = 70,
            FieldGoalsMade = 22,
            FieldGoalsAttempted = 50,
            ThreePointsMade = 6,
            ThreePointsAttempted = 20,
            OffensiveRebounds = 8,
            DefensiveRebounds = 25,
            Turnovers = 16,
            PersonalFouls = 18
        };

        var diagnostics = GameDiagnosticsCalculator.Calculate(team, opponent, "Amateur_Default");

        Assert.Equal(10, diagnostics.PointsDiff);
        Assert.Equal(-4, diagnostics.TurnoversDiff);
        Assert.Equal(2, diagnostics.OffensiveReboundsDiff);
        Assert.Equal(5, diagnostics.DefensiveReboundsDiff);
        Assert.Equal(-3, diagnostics.FoulsDiff);
        Assert.Equal(0, diagnostics.ThreePointAttemptsDiff);
        Assert.Equal(0.50, diagnostics.TeamFieldGoalPercentage, precision: 10);
        Assert.Equal(0.44, diagnostics.OpponentFieldGoalPercentage, precision: 10);
        Assert.Equal(0.06, diagnostics.FieldGoalPctDiff, precision: 10);
        Assert.Equal(0.10, diagnostics.ThreePointPctDiff, precision: 10);
        Assert.Equal("Amateur_Default", diagnostics.AppliedRulesProfile);
    }

    [Fact]
    public void Calculate_ZeroAttempts_ProducesZeroPercentageRatherThanDivideByZero()
    {
        var team = new TeamStats { Points = 0, FieldGoalsAttempted = 0, ThreePointsAttempted = 0 };
        var opponent = new TeamStats { Points = 0, FieldGoalsAttempted = 0, ThreePointsAttempted = 0 };

        var diagnostics = GameDiagnosticsCalculator.Calculate(team, opponent, "Fallback_Default");

        Assert.Equal(0.0, diagnostics.TeamFieldGoalPercentage);
        Assert.Equal(0.0, diagnostics.OpponentFieldGoalPercentage);
        Assert.Equal(0.0, diagnostics.FieldGoalPctDiff);
        Assert.Equal(0.0, diagnostics.ThreePointPctDiff);
    }
}
