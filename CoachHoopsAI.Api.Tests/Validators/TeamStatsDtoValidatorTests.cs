using CoachHoopsAI.Api.Contracts;
using CoachHoopsAI.Api.Validators;

namespace CoachHoopsAI.Api.Tests.Validators;

public class TeamStatsDtoValidatorTests
{
    private readonly TeamStatsDtoValidator _validator = new();

    private static TeamStatsDto Valid() => new()
    {
        Points = 80,
        FieldGoalsMade = 30,
        FieldGoalsAttempted = 60,
        ThreePointsMade = 8,
        ThreePointsAttempted = 20,
        FreeThrowsMade = 12,
        FreeThrowsAttempted = 16,
        OffensiveRebounds = 10,
        DefensiveRebounds = 25,
        Assists = 18,
        Turnovers = 12,
        Steals = 6,
        Blocks = 3,
        PersonalFouls = 15
    };

    [Fact]
    public void Validate_ValidStats_Passes()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Validate_AllZeroStatLine_Passes()
    {
        // Zero values are legitimate, e.g. at the start of a live game.
        Assert.True(_validator.Validate(new TeamStatsDto()).IsValid);
    }

    public static IEnumerable<object[]> NegativeCountMutators()
    {
        yield return new object[] { (Action<TeamStatsDto>)(d => d.Points = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.FieldGoalsMade = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.FieldGoalsAttempted = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.ThreePointsMade = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.ThreePointsAttempted = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.FreeThrowsMade = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.FreeThrowsAttempted = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.OffensiveRebounds = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.DefensiveRebounds = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.Assists = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.Turnovers = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.Steals = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.Blocks = -1) };
        yield return new object[] { (Action<TeamStatsDto>)(d => d.PersonalFouls = -1) };
    }

    [Theory]
    [MemberData(nameof(NegativeCountMutators))]
    public void Validate_AnyNegativeCount_Fails(Action<TeamStatsDto> mutate)
    {
        var dto = Valid();
        mutate(dto);

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_FieldGoalsMadeGreaterThanAttempted_Fails()
    {
        var dto = Valid();
        dto.FieldGoalsMade = dto.FieldGoalsAttempted + 1;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_ThreePointsMadeGreaterThanAttempted_Fails()
    {
        var dto = Valid();
        dto.ThreePointsMade = dto.ThreePointsAttempted + 1;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_FreeThrowsMadeGreaterThanAttempted_Fails()
    {
        var dto = Valid();
        dto.FreeThrowsMade = dto.FreeThrowsAttempted + 1;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_ThreePointsMadeGreaterThanFieldGoalsMade_Fails()
    {
        var dto = Valid();
        // 3PM can never exceed FGM - a three is also a field goal.
        dto.FieldGoalsMade = dto.ThreePointsMade - 1;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_ThreePointsAttemptedGreaterThanFieldGoalsAttempted_Fails()
    {
        var dto = Valid();
        dto.FieldGoalsAttempted = dto.ThreePointsAttempted - 1;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_MadeEqualsAttempted_Passes()
    {
        var dto = Valid();
        dto.FieldGoalsMade = dto.FieldGoalsAttempted;
        dto.ThreePointsMade = dto.ThreePointsAttempted;
        dto.FreeThrowsMade = dto.FreeThrowsAttempted;

        Assert.True(_validator.Validate(dto).IsValid);
    }
}
