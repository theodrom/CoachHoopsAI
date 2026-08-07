using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Application.Services;
using CoachHoopsAI.Application.Tests.TestDoubles;
using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using CoachHoopsAI.Domain.Rules;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Application.Tests.Services;

// Orchestration tests for GameAnalysisService using hand-written fakes instead of
// a mocking framework. These verify the *wiring* (who gets called with what, and
// how the result is assembled) rather than re-testing rule thresholds, which are
// covered separately in CoachHoopsAI.Domain.Tests.
public class GameAnalysisServiceTests
{
    private static TeamStats Stats(int points = 80) => new()
    {
        Points = points,
        FieldGoalsMade = 30,
        FieldGoalsAttempted = 60,
        ThreePointsMade = 7,
        ThreePointsAttempted = 20,
        OffensiveRebounds = 10,
        DefensiveRebounds = 30,
        Turnovers = 12,
        PersonalFouls = 15
    };

    private static GameAnalysisInput Input(TeamStats? team = null, TeamStats? opponent = null) =>
        new(Level.Amateur, team ?? Stats(80), opponent ?? Stats(70), "coach notes", metadata: null, rulesProfile: null);

    [Fact]
    public async Task AnalyzeAsync_PassesResolvedProfileAndBothTeamsToRulesEngine()
    {
        var rulesEngine = new FakeStatRulesEngine();
        var llmClient = new FakeLlmSuggestionClient();
        var profileProvider = new FakeRulesProfileProvider();
        var resolvedProfile = new ResolvedRulesProfile("Amateur_Default", new RulesProfile { TurnoverDiffToFlag = 99 });
        profileProvider.ProfileToReturn = resolvedProfile;

        var sut = new GameAnalysisService(rulesEngine, llmClient, profileProvider);
        var team = Stats(80);
        var opponent = Stats(70);

        await sut.AnalyzeAsync(Input(team, opponent));

        Assert.Equal(1, rulesEngine.CallCount);
        Assert.Same(team, rulesEngine.LastTeam);
        Assert.Same(opponent, rulesEngine.LastOpponent);
        Assert.Same(resolvedProfile.Profile, rulesEngine.LastProfile);
    }

    [Fact]
    public async Task AnalyzeAsync_PassesLevelAndOverrideToProfileProvider()
    {
        var rulesEngine = new FakeStatRulesEngine();
        var llmClient = new FakeLlmSuggestionClient();
        var profileProvider = new FakeRulesProfileProvider();
        var sut = new GameAnalysisService(rulesEngine, llmClient, profileProvider);

        var input = new GameAnalysisInput(Level.Pro, Stats(), Stats(), "", metadata: null, rulesProfile: "Pro_Aggressive");

        await sut.AnalyzeAsync(input);

        Assert.Equal(Level.Pro, profileProvider.LastLevel);
        Assert.Equal("Pro_Aggressive", profileProvider.LastOverrideProfileName);
    }

    [Fact]
    public async Task AnalyzeAsync_PassesRulesEngineTagsAndDiagnosticsIntoLlmClient()
    {
        var rulesEngine = new FakeStatRulesEngine
        {
            TagsToReturn = new[] { ProblemTag.TurnoverProblem, ProblemTag.FoulsProblem }
        };
        var llmClient = new FakeLlmSuggestionClient();
        var profileProvider = new FakeRulesProfileProvider
        {
            ProfileToReturn = new ResolvedRulesProfile("Youth_Default", new RulesProfile())
        };
        var sut = new GameAnalysisService(rulesEngine, llmClient, profileProvider);
        var team = Stats(80);
        var opponent = Stats(70);

        await sut.AnalyzeAsync(Input(team, opponent));

        Assert.Equal(1, llmClient.CallCount);
        Assert.Equal(rulesEngine.TagsToReturn, llmClient.LastTags);
        Assert.Equal("Youth_Default", llmClient.LastAppliedRulesProfile);
        Assert.NotNull(llmClient.LastDiagnostics);
        // Diagnostics are computed by the real GameDiagnosticsCalculator from the same stats.
        Assert.Equal(team.Points - opponent.Points, llmClient.LastDiagnostics!.PointsDiff);
        Assert.Equal("Youth_Default", llmClient.LastDiagnostics!.AppliedRulesProfile);
    }

    [Fact]
    public async Task AnalyzeAsync_AssemblesResultFromLevelTagsSuggestionsAndDiagnostics()
    {
        var expectedTags = new[] { ProblemTag.OpponentHotFromThree };
        var expectedSuggestions = new[]
        {
            new Suggestion(SuggestionCategory.Defense, "Close out harder on shooters.", "OpponentHotFromThree tag fired.")
        };
        var rulesEngine = new FakeStatRulesEngine { TagsToReturn = expectedTags };
        var llmClient = new FakeLlmSuggestionClient { SuggestionsToReturn = expectedSuggestions };
        var profileProvider = new FakeRulesProfileProvider();
        var sut = new GameAnalysisService(rulesEngine, llmClient, profileProvider);

        var result = await sut.AnalyzeAsync(new GameAnalysisInput(Level.Youth, Stats(), Stats(), ""));

        Assert.Equal(Level.Youth, result.Level);
        Assert.Equal(expectedTags, result.ProblemTags);
        Assert.Equal(expectedSuggestions, result.Suggestions);
        Assert.NotNull(result.Diagnostics);
    }

    [Fact]
    public async Task AnalyzeAsync_NoTagsDetected_StillCallsLlmClientWithEmptyTagCollection()
    {
        var rulesEngine = new FakeStatRulesEngine { TagsToReturn = Array.Empty<ProblemTag>() };
        var llmClient = new FakeLlmSuggestionClient();
        var profileProvider = new FakeRulesProfileProvider();
        var sut = new GameAnalysisService(rulesEngine, llmClient, profileProvider);

        var result = await sut.AnalyzeAsync(Input());

        Assert.Equal(1, llmClient.CallCount);
        Assert.Empty(llmClient.LastTags!);
        Assert.Empty(result.ProblemTags!);
    }
}
