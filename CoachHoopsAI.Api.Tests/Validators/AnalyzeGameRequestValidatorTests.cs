using CoachHoopsAI.Api.Contracts;
using CoachHoopsAI.Api.Validators;

namespace CoachHoopsAI.Api.Tests.Validators;

public class AnalyzeGameRequestValidatorTests
{
    private readonly AnalyzeGameRequestValidator _validator = new(
        new TeamStatsDtoValidator(),
        new GameFormatDtoValidator(),
        new GameTimingDtoValidator());

    private static TeamStatsDto ValidStats() => new()
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

    private static GameFormatDto ValidGameFormat() => new()
    {
        RegulationPeriods = 4,
        RegulationPeriodMinutes = 10,
        OvertimePeriodMinutes = 5
    };

    private static GameTimingDto ValidGameTiming() => new()
    {
        CurrentPeriod = 3,
        ClockRemainingSeconds = 300 // 5:00, within the 10-minute regulation period
    };

    private static AnalyzeGameRequest ValidRequest() => new()
    {
        Team = ValidStats(),
        Opponent = ValidStats(),
        Level = "Amateur",
        GameFormat = ValidGameFormat(),
        GameTiming = ValidGameTiming()
    };

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MissingTeam_Fails()
    {
        var request = ValidRequest();
        request.Team = null;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_MissingOpponent_Fails()
    {
        var request = ValidRequest();
        request.Opponent = null;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_InvalidTeamStats_PropagatesNestedValidationFailure()
    {
        var request = ValidRequest();
        request.Team!.FieldGoalsMade = request.Team.FieldGoalsAttempted + 1; // invalid on the nested TeamStatsDto

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("EasyBasket")]
    [InlineData("Youth")]
    [InlineData("Amateur")]
    [InlineData("Pro")]
    [InlineData("pro")]
    [InlineData("easy-basket")]
    [InlineData("easy_basket")]
    public void Validate_AllowedLevelVariants_Pass(string level)
    {
        var request = ValidRequest();
        request.Level = level;

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Semi-Pro")]
    [InlineData("College")]
    public void Validate_DisallowedLevel_Fails(string? level)
    {
        var request = ValidRequest();
        request.Level = level;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_GameDateBefore1900_Fails()
    {
        var request = ValidRequest();
        request.GameDate = new DateTime(1899, 12, 31);

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_NullGameDate_Passes()
    {
        var request = ValidRequest();
        request.GameDate = null;

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_NotesTooLong_Fails()
    {
        var request = ValidRequest();
        request.Notes = new string('x', 2001);

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_NotesAtMaxLength_Passes()
    {
        var request = ValidRequest();
        request.Notes = new string('x', 2000);

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_MissingGameFormat_Fails()
    {
        var request = ValidRequest();
        request.GameFormat = null;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_MissingGameTiming_Fails()
    {
        var request = ValidRequest();
        request.GameTiming = null;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0, 10, 5)]  // RegulationPeriods = 0
    [InlineData(4, 0, 5)]   // RegulationPeriodMinutes = 0
    [InlineData(4, 10, 0)]  // OvertimePeriodMinutes = 0
    public void Validate_InvalidGameFormatValues_Fails(int regulationPeriods, int regulationPeriodMinutes, int overtimePeriodMinutes)
    {
        var request = ValidRequest();
        request.GameFormat = new GameFormatDto
        {
            RegulationPeriods = regulationPeriods,
            RegulationPeriodMinutes = regulationPeriodMinutes,
            OvertimePeriodMinutes = overtimePeriodMinutes
        };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_CurrentPeriodZero_Fails()
    {
        var request = ValidRequest();
        request.GameTiming = new GameTimingDto { CurrentPeriod = 0, ClockRemainingSeconds = 300 };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_NegativeClockRemaining_Fails()
    {
        var request = ValidRequest();
        request.GameTiming = new GameTimingDto { CurrentPeriod = 1, ClockRemainingSeconds = -1 };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_RegulationClockExceedsRegulationPeriodLength_Fails()
    {
        // GameFormat 4x10, period 3 (regulation), clock shows 11:42 - more than a
        // 10-minute period can ever have remaining.
        var request = ValidRequest();
        request.GameFormat = new GameFormatDto { RegulationPeriods = 4, RegulationPeriodMinutes = 10, OvertimePeriodMinutes = 5 };
        request.GameTiming = new GameTimingDto { CurrentPeriod = 3, ClockRemainingSeconds = 702 };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_OvertimeClockExceedsOvertimePeriodLength_Fails()
    {
        // GameFormat 4x10 with 5-minute OT; period 5 = OT1, clock shows 5:01.
        var request = ValidRequest();
        request.GameFormat = new GameFormatDto { RegulationPeriods = 4, RegulationPeriodMinutes = 10, OvertimePeriodMinutes = 5 };
        request.GameTiming = new GameTimingDto { CurrentPeriod = 5, ClockRemainingSeconds = 301 };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_OvertimeClockWithinOvertimePeriodLength_Passes()
    {
        var request = ValidRequest();
        request.GameFormat = new GameFormatDto { RegulationPeriods = 4, RegulationPeriodMinutes = 10, OvertimePeriodMinutes = 5 };
        request.GameTiming = new GameTimingDto { CurrentPeriod = 5, ClockRemainingSeconds = 300 };

        Assert.True(_validator.Validate(request).IsValid);
    }
}
