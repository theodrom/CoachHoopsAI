using CoachHoopsAI.Api.Contracts;
using CoachHoopsAI.Api.Validators;

namespace CoachHoopsAI.Api.Tests.Validators;

public class AnalyzeGameRequestValidatorTests
{
    private readonly AnalyzeGameRequestValidator _validator = new(new TeamStatsDtoValidator());

    private static TeamStatsDto ValidStats() => new()
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

    private static AnalyzeGameRequest ValidRequest() => new()
    {
        Team = ValidStats(),
        Opponent = ValidStats(),
        Level = "Amateur"
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
        request.Team!.FieldGoalPercentage = 1.5; // invalid on the nested TeamStatsDto

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
}
