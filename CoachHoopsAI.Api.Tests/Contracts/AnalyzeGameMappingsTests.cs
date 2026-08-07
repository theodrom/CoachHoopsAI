using System.Reflection;
using CoachHoopsAI.Api.Contracts;
using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Api.Tests.Contracts;

// Regression coverage for the request->domain and result->response mapping in
// AnalyzeGameMappings. The reflection-based test below is deliberately written so
// that it FAILS if a TeamStatsDto property is ever added without a matching,
// correctly-mapped property on the domain TeamStats entity - the exact class of bug
// that broke the build before Milestone 0: a DTO/domain shape drifting apart.
public class AnalyzeGameMappingsTests
{
    private static TeamStatsDto SampleTeamDto() => new()
    {
        Points = 111,
        FieldGoalsMade = 41,
        FieldGoalsAttempted = 85,
        ThreePointsMade = 12,
        ThreePointsAttempted = 30,
        FreeThrowsMade = 17,
        FreeThrowsAttempted = 21,
        OffensiveRebounds = 9,
        DefensiveRebounds = 33,
        Assists = 24,
        Turnovers = 13,
        Steals = 8,
        Blocks = 4,
        PersonalFouls = 17
    };

    private static TeamStatsDto SampleOpponentDto() => new()
    {
        Points = 98,
        FieldGoalsMade = 36,
        FieldGoalsAttempted = 80,
        ThreePointsMade = 9,
        ThreePointsAttempted = 26,
        FreeThrowsMade = 17,
        FreeThrowsAttempted = 22,
        OffensiveRebounds = 11,
        DefensiveRebounds = 27,
        Assists = 19,
        Turnovers = 15,
        Steals = 6,
        Blocks = 2,
        PersonalFouls = 19
    };

    private static GameFormatDto SampleGameFormatDto() => new()
    {
        RegulationPeriods = 4,
        RegulationPeriodMinutes = 10,
        OvertimePeriodMinutes = 5,
        Name = "FIBA"
    };

    private static GameTimingDto SampleGameTimingDto() => new()
    {
        CurrentPeriod = 3,
        ClockRemainingSeconds = 300
    };

    private static AnalyzeGameRequest ValidRequest() => new()
    {
        Team = SampleTeamDto(),
        Opponent = SampleOpponentDto(),
        Level = "Amateur",
        GameFormat = SampleGameFormatDto(),
        GameTiming = SampleGameTimingDto()
    };

    [Fact]
    public void ToGameAnalysisInput_MapsEveryTeamStatsDtoPropertyOntoDomainTeamStatsByName()
    {
        var dto = SampleTeamDto();
        var request = ValidRequest();
        request.Team = dto;

        var input = request.ToGameAnalysisInput();

        AssertAllPropertiesMapped(dto, input.Team);
    }

    [Fact]
    public void ToGameAnalysisInput_MapsEveryOpponentStatsDtoPropertyOntoDomainTeamStatsByName()
    {
        var dto = SampleOpponentDto();
        var request = ValidRequest();
        request.Opponent = dto;

        var input = request.ToGameAnalysisInput();

        AssertAllPropertiesMapped(dto, input.Opponent);
    }

    private static void AssertAllPropertiesMapped(TeamStatsDto dto, TeamStats mapped)
    {
        foreach (var dtoProp in typeof(TeamStatsDto).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var domainProp = typeof(TeamStats).GetProperty(dtoProp.Name, BindingFlags.Public | BindingFlags.Instance);
            Assert.True(domainProp is not null,
                $"TeamStatsDto.{dtoProp.Name} has no matching property on domain TeamStats - mapping is now incomplete.");

            var expected = dtoProp.GetValue(dto);
            var actual = domainProp!.GetValue(mapped);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void ToGameAnalysisInput_NullTeamAndOpponent_DefaultsToZeroedStats()
    {
        var request = ValidRequest();
        request.Team = null;
        request.Opponent = null;

        var input = request.ToGameAnalysisInput();

        Assert.Equal(0, input.Team.Points);
        Assert.Equal(0, input.Opponent.Points);
        Assert.Equal(0, input.Team.FieldGoalsAttempted);
    }

    [Fact]
    public void ToGameAnalysisInput_MapsEveryGameFormatDtoPropertyOntoDomainGameFormat()
    {
        var dto = SampleGameFormatDto();
        var request = ValidRequest();
        request.GameFormat = dto;

        var input = request.ToGameAnalysisInput();

        Assert.NotNull(input.GameFormat);
        Assert.Equal(dto.RegulationPeriods, input.GameFormat!.RegulationPeriods);
        Assert.Equal(dto.RegulationPeriodMinutes, input.GameFormat!.RegulationPeriodMinutes);
        Assert.Equal(dto.OvertimePeriodMinutes, input.GameFormat!.OvertimePeriodMinutes);
        Assert.Equal(dto.Name, input.GameFormat!.Name);
    }

    [Fact]
    public void ToGameAnalysisInput_NullGameFormat_MapsToNull()
    {
        var request = ValidRequest();
        request.GameFormat = null;

        var input = request.ToGameAnalysisInput();

        Assert.Null(input.GameFormat);
    }

    [Fact]
    public void ToGameAnalysisInput_MapsGameTimingClockRemainingSecondsToTimeSpan()
    {
        var dto = SampleGameTimingDto();
        var request = ValidRequest();
        request.GameTiming = dto;

        var input = request.ToGameAnalysisInput();

        Assert.NotNull(input.GameTiming);
        Assert.Equal(dto.CurrentPeriod, input.GameTiming!.CurrentPeriod);
        Assert.Equal(TimeSpan.FromSeconds(dto.ClockRemainingSeconds), input.GameTiming!.ClockRemaining);
    }

    [Fact]
    public void ToGameAnalysisInput_NullGameTiming_MapsToNull()
    {
        var request = ValidRequest();
        request.GameTiming = null;

        var input = request.ToGameAnalysisInput();

        Assert.Null(input.GameTiming);
    }

    [Theory]
    [InlineData("EasyBasket", Level.EasyBasket)]
    [InlineData("easybasket", Level.EasyBasket)]
    [InlineData("Youth", Level.Youth)]
    [InlineData("Amateur", Level.Amateur)]
    [InlineData("Pro", Level.Pro)]
    [InlineData("professional", Level.Pro)]
    [InlineData("", Level.Amateur)]        // documented fallback
    [InlineData("not-a-level", Level.Amateur)] // documented fallback
    public void ToGameAnalysisInput_ParsesLevelStringWithDocumentedFallback(string levelText, Level expected)
    {
        var request = ValidRequest();
        request.Level = levelText;

        var input = request.ToGameAnalysisInput();

        Assert.Equal(expected, input.Level);
    }

    [Fact]
    public void ToGameAnalysisInput_MapsMetadataAndNotes()
    {
        var request = ValidRequest();
        request.Notes = "Struggled with their PnR.";
        request.TeamName = "  Falcons  ";
        request.OpponentName = "Hawks";
        request.Season = "2025/2026";
        request.Location = "Home Arena";

        var input = request.ToGameAnalysisInput();

        Assert.Equal("Struggled with their PnR.", input.Notes);
        Assert.Equal("Falcons", input.Metadata!.TeamName); // trimmed
        Assert.Equal("Hawks", input.Metadata!.OpponentName);
        Assert.Equal("2025/2026", input.Metadata!.Season);
        Assert.Equal("Home Arena", input.Metadata!.Location);
    }

    [Fact]
    public void ToResponseDto_MapsTagsToStringsAndSuggestionsIntoCategoryBuckets()
    {
        var result = new GameAnalysisResult
        {
            Level = Level.Amateur,
            ProblemTags = new[] { ProblemTag.TurnoverProblem, ProblemTag.FoulsProblem },
            Suggestions = new[]
            {
                new Suggestion(SuggestionCategory.Offense, "Run more ball screens.", "OurShootingInefficiency"),
                new Suggestion(SuggestionCategory.Defense, "Get back in transition.", "TransitionDefenseProblem"),
                new Suggestion(SuggestionCategory.Other, "Manage fouls.", "FoulsProblem")
            },
            Diagnostics = new GameDiagnostics(5, -2, 1, 3, 0.05, 2, -1, 0.5, 0.45, 0.05, "Amateur_Default")
        };

        var dto = result.ToResponseDto();

        Assert.Equal(new[] { "TurnoverProblem", "FoulsProblem" }, dto.ProblemTags);
        Assert.Single(dto.Suggestions.Offense!);
        Assert.Single(dto.Suggestions.Defense!);
        Assert.Single(dto.Suggestions.Other!);
        Assert.Equal("Run more ball screens.", dto.Suggestions.Offense!.First().Suggestion);
        Assert.Equal(5, dto.Diagnostics.PointsDiff);
        Assert.Equal("Amateur_Default", dto.Diagnostics.AppliedRulesProfile);
    }
}
