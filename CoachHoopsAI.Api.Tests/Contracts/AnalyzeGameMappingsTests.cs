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
// correctly-mapped property on the domain TeamStats entity (the exact class of bug
// that broke the build before this milestone: a DTO/domain shape drifting apart).
public class AnalyzeGameMappingsTests
{
    private static TeamStatsDto SampleTeamDto() => new()
    {
        Points = 111,
        FieldGoalPercentage = 0.512,
        ThreePointPercentage = 0.411,
        ThreePointAttempts = 21,
        OffensiveRebounds = 9,
        DefensiveRebounds = 33,
        Turnovers = 13,
        Fouls = 17
    };

    private static TeamStatsDto SampleOpponentDto() => new()
    {
        Points = 98,
        FieldGoalPercentage = 0.44,
        ThreePointPercentage = 0.33,
        ThreePointAttempts = 17,
        OffensiveRebounds = 11,
        DefensiveRebounds = 27,
        Turnovers = 15,
        Fouls = 19
    };

    [Fact]
    public void ToGameAnalysisInput_MapsEveryTeamStatsDtoPropertyOntoDomainTeamStatsByName()
    {
        var dto = SampleTeamDto();
        var request = new AnalyzeGameRequest { Team = dto, Opponent = SampleOpponentDto(), Level = "Amateur" };

        var input = request.ToGameAnalysisInput();

        AssertAllPropertiesMapped(dto, input.Team);
    }

    [Fact]
    public void ToGameAnalysisInput_MapsEveryOpponentStatsDtoPropertyOntoDomainTeamStatsByName()
    {
        var dto = SampleOpponentDto();
        var request = new AnalyzeGameRequest { Team = SampleTeamDto(), Opponent = dto, Level = "Amateur" };

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
        var request = new AnalyzeGameRequest { Team = null, Opponent = null, Level = "Amateur" };

        var input = request.ToGameAnalysisInput();

        Assert.Equal(0, input.Team.Points);
        Assert.Equal(0, input.Opponent.Points);
        Assert.Equal(0.0, input.Team.FieldGoalPercentage);
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
        var request = new AnalyzeGameRequest { Team = SampleTeamDto(), Opponent = SampleOpponentDto(), Level = levelText };

        var input = request.ToGameAnalysisInput();

        Assert.Equal(expected, input.Level);
    }

    [Fact]
    public void ToGameAnalysisInput_MapsMetadataAndNotes()
    {
        var request = new AnalyzeGameRequest
        {
            Team = SampleTeamDto(),
            Opponent = SampleOpponentDto(),
            Level = "Pro",
            Notes = "Struggled with their PnR.",
            TeamName = "  Falcons  ",
            OpponentName = "Hawks",
            Season = "2025/2026",
            Location = "Home Arena"
        };

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
