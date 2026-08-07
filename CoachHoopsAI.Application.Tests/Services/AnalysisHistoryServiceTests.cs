using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Application.Services;
using CoachHoopsAI.Application.Tests.TestDoubles;
using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Application.Tests.Services;

// Regression coverage for AnalysisHistoryService.AnalyzeAndStoreAsync building the
// AnalysisRecord it persists - in particular Season/Location/AiModel, which were
// previously dropped (Season/Location) or hardcoded to "" (AiModel) rather than
// taken from the request/analysis result.
public class AnalysisHistoryServiceTests
{
    private static TeamStats Stats() => new() { Points = 80, FieldGoalsMade = 30, FieldGoalsAttempted = 60 };

    private static GameAnalysisInput InputWithMetadata(string? season, string? location) =>
        new(Level.Amateur, Stats(), Stats(), "notes",
            metadata: new GameMetadata(
                gameDate: null, teamName: "Falcons", opponentName: "Hawks",
                competition: null, season: season, location: location));

    [Fact]
    public async Task AnalyzeAndStoreAsync_SeasonSupplied_IsPersisted()
    {
        var analysisService = new FakeGameAnalysisService { ResultToReturn = new GameAnalysisResult { AiModel = "gpt-4.1-mini" } };
        var repo = new FakeAnalysisRepository();
        var sut = new AnalysisHistoryService(analysisService, repo);

        await sut.AnalyzeAndStoreAsync(InputWithMetadata(season: "2025/2026", location: null));

        Assert.Equal("2025/2026", repo.LastSavedRecord!.Season);
    }

    [Fact]
    public async Task AnalyzeAndStoreAsync_LocationSupplied_IsPersisted()
    {
        var analysisService = new FakeGameAnalysisService { ResultToReturn = new GameAnalysisResult { AiModel = "gpt-4.1-mini" } };
        var repo = new FakeAnalysisRepository();
        var sut = new AnalysisHistoryService(analysisService, repo);

        await sut.AnalyzeAndStoreAsync(InputWithMetadata(season: null, location: "Home Arena"));

        Assert.Equal("Home Arena", repo.LastSavedRecord!.Location);
    }

    [Fact]
    public async Task AnalyzeAndStoreAsync_NullSeasonAndLocationInMetadata_RemainNull()
    {
        var analysisService = new FakeGameAnalysisService { ResultToReturn = new GameAnalysisResult() };
        var repo = new FakeAnalysisRepository();
        var sut = new AnalysisHistoryService(analysisService, repo);

        await sut.AnalyzeAndStoreAsync(InputWithMetadata(season: null, location: null));

        Assert.Null(repo.LastSavedRecord!.Season);
        Assert.Null(repo.LastSavedRecord!.Location);
    }

    [Fact]
    public async Task AnalyzeAndStoreAsync_NoMetadataAtAll_SeasonAndLocationRemainNull()
    {
        var analysisService = new FakeGameAnalysisService { ResultToReturn = new GameAnalysisResult() };
        var repo = new FakeAnalysisRepository();
        var sut = new AnalysisHistoryService(analysisService, repo);

        var input = new GameAnalysisInput(Level.Amateur, Stats(), Stats(), "notes", metadata: null);

        await sut.AnalyzeAndStoreAsync(input);

        Assert.Null(repo.LastSavedRecord!.Season);
        Assert.Null(repo.LastSavedRecord!.Location);
    }

    [Fact]
    public async Task AnalyzeAndStoreAsync_PersistsAiModelFromAnalysisResult()
    {
        var analysisService = new FakeGameAnalysisService { ResultToReturn = new GameAnalysisResult { AiModel = "gpt-4.1-mini" } };
        var repo = new FakeAnalysisRepository();
        var sut = new AnalysisHistoryService(analysisService, repo);

        await sut.AnalyzeAndStoreAsync(InputWithMetadata(season: null, location: null));

        Assert.Equal("gpt-4.1-mini", repo.LastSavedRecord!.AiModel);
    }

    [Fact]
    public async Task AnalyzeAndStoreAsync_ExistingBehaviorStillWorks()
    {
        var diagnostics = new GameDiagnostics(5, -2, 1, 3, 0.05, 2, -1, 0.5, 0.45, 0.05, "Amateur_Default");
        var analysisService = new FakeGameAnalysisService
        {
            ResultToReturn = new GameAnalysisResult
            {
                Level = Level.Amateur,
                ProblemTags = new[] { ProblemTag.TurnoverProblem },
                Suggestions = Array.Empty<Suggestion>(),
                Diagnostics = diagnostics,
                AiModel = "gpt-4.1-mini"
            }
        };
        var repo = new FakeAnalysisRepository { IdToReturn = Guid.NewGuid() };
        var sut = new AnalysisHistoryService(analysisService, repo);

        var input = new GameAnalysisInput(
            Level.Amateur, Stats(), Stats(), "coach notes",
            metadata: new GameMetadata(
                gameDate: null, teamName: "Falcons", opponentName: "Hawks",
                competition: null, season: "2025/2026", location: "Home Arena"),
            rulesProfile: "Amateur_Default");

        var (result, id) = await sut.AnalyzeAndStoreAsync(input);

        Assert.Equal(1, repo.SaveCallCount);
        Assert.Equal(repo.IdToReturn, id);
        Assert.Same(analysisService.ResultToReturn, result);
        Assert.Equal("Amateur", repo.LastSavedRecord!.Level);
        Assert.Equal("Amateur_Default", repo.LastSavedRecord!.RequestedRulesProfile);
        Assert.Equal("Amateur_Default", repo.LastSavedRecord!.AppliedRulesProfile);
        Assert.Equal("Falcons", repo.LastSavedRecord!.TeamName);
        Assert.Equal("Hawks", repo.LastSavedRecord!.OpponentName);
        Assert.Equal("2025/2026", repo.LastSavedRecord!.Season);
        Assert.Equal("Home Arena", repo.LastSavedRecord!.Location);
        Assert.Equal("gpt-4.1-mini", repo.LastSavedRecord!.AiModel);
        Assert.False(string.IsNullOrWhiteSpace(repo.LastSavedRecord!.InputJson));
    }
}
