using CoachHoopsAI.Application.Services;
using CoachHoopsAI.Domain.Entities;

namespace CoachHoopsAI.Application.Tests.Services;

public class GameDiagnosticsCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsTeamMinusOpponentDiffsAndPassesThroughAppliedProfile()
    {
        var team = new TeamStats(points: 80, fieldGoalPercentage: 0.50, threePointPercentage: 0.40,
            threePointAttempts: 20, offensiveRebounds: 10, defensiveRebounds: 30, turnovers: 12, fouls: 15);
        var opponent = new TeamStats(points: 70, fieldGoalPercentage: 0.44, threePointPercentage: 0.30,
            threePointAttempts: 15, offensiveRebounds: 8, defensiveRebounds: 25, turnovers: 16, fouls: 18);

        var diagnostics = GameDiagnosticsCalculator.Calculate(team, opponent, "Amateur_Default");

        Assert.Equal(10, diagnostics.PointsDiff);
        Assert.Equal(-4, diagnostics.TurnoversDiff);
        Assert.Equal(2, diagnostics.OffensiveReboundsDiff);
        Assert.Equal(5, diagnostics.DefensiveReboundsDiff);
        Assert.Equal(-3, diagnostics.FoulsDiff);
        Assert.Equal(5, diagnostics.ThreePointAttemptsDiff);
        Assert.Equal(0.50, diagnostics.TeamFieldGoalPercentage);
        Assert.Equal(0.44, diagnostics.OpponentFieldGoalPercentage);
        Assert.Equal(0.06, diagnostics.FieldGoalPctDiff, precision: 10);
        Assert.Equal(0.10, diagnostics.ThreePointPctDiff, precision: 10);
        Assert.Equal("Amateur_Default", diagnostics.AppliedRulesProfile);
    }
}
