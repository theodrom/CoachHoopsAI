using CoachHoopsAI.Api.Contracts;
using CoachHoopsAI.Api.Validators;

namespace CoachHoopsAI.Api.Tests.Validators;

public class TeamStatsDtoValidatorTests
{
    private readonly TeamStatsDtoValidator _validator = new();

    private static TeamStatsDto Valid() => new()
    {
        Points = 80,
        FieldGoalPercentage = 0.45,
        ThreePointPercentage = 0.35,
        ThreePointAttempts = 20,
        OffensiveRebounds = 10,
        DefensiveRebounds = 25,
        Turnovers = 12,
        Fouls = 15
    };

    [Fact]
    public void Validate_ValidStats_Passes()
    {
        var result = _validator.Validate(Valid());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    public void Validate_NegativePoints_Fails(int points)
    {
        var dto = Valid();
        dto.Points = points;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Validate_FieldGoalPercentageOutOfRange_Fails(double pct)
    {
        var dto = Valid();
        dto.FieldGoalPercentage = pct;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void Validate_FieldGoalPercentageAtInclusiveBounds_Passes(double pct)
    {
        var dto = Valid();
        dto.FieldGoalPercentage = pct;

        Assert.True(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_ZeroThreePointAttemptsWithNonZeroPercentage_Fails()
    {
        var dto = Valid();
        dto.ThreePointAttempts = 0;
        dto.ThreePointPercentage = 0.5;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_ZeroThreePointAttemptsWithZeroPercentage_Passes()
    {
        var dto = Valid();
        dto.ThreePointAttempts = 0;
        dto.ThreePointPercentage = 0.0;

        Assert.True(_validator.Validate(dto).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    public void Validate_NegativeTurnovers_Fails(int turnovers)
    {
        var dto = Valid();
        dto.Turnovers = turnovers;

        Assert.False(_validator.Validate(dto).IsValid);
    }
}
