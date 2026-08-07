using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using CoachHoopsAI.Infrastructure.AI;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Infrastructure.Tests.AI;

// Regression coverage for the internal-identifier filter added on top of
// OpenAiSuggestionClientHttp's response parsing: a suggestion is dropped if its
// "text" or "reason" contains, verbatim, one of the ProblemTag names or the
// rules-profile name(s) this specific request exposed to the model.
//
// These tests only exercise the public GetSuggestionsAsync surface against a fake
// HTTP handler returning a canned Responses API body - the prompt, the real OpenAI
// service, the Responses API itself, and JSON serialization internals are not under
// test. Only the observable filtering behavior is: what goes in as "model output",
// what comes back out as Suggestions.
public class OpenAiSuggestionClientIdentifierFilterTests
{
    private static OpenAiSuggestionClientHttp CreateClient(string responseBody)
    {
        var httpClient = new HttpClient(new FakeResponsesApiHandler(responseBody));
        var options = Microsoft.Extensions.Options.Options.Create(new OpenAiOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            BaseUrl = "http://localhost"
        });

        return new OpenAiSuggestionClientHttp(options, httpClient);
    }

    private static GameAnalysisInput CreateInput(string? rulesProfile = null) =>
        new(Level.Amateur, new TeamStats(), new TeamStats(), "notes", metadata: null, rulesProfile: rulesProfile);

    private static GameDiagnostics CreateDiagnostics(string appliedRulesProfile) =>
        new(pointsDiff: 0, turnoversDiff: 0, offensiveReboundsDiff: 0, defensiveReboundsDiff: 0,
            threePointPctDiff: 0.0, threePointAttemptsDiff: 0, foulsDiff: 0,
            teamFieldGoalPercentage: 0.0, opponentFieldGoalPercentage: 0.0, fieldGoalPctDiff: 0.0,
            appliedRulesProfile: appliedRulesProfile);

    // 1. Clean suggestion passes.
    [Fact]
    public async Task CleanSuggestion_TextAndReasonArePreserved()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Defense", "Work on defensive closeouts.", "The opponent has been shooting well from three."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.OpponentHotFromThree },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Work on defensive closeouts.", suggestion.Text);
        Assert.Equal("The opponent has been shooting well from three.", suggestion.Reason);
    }

    // 2. Internal ProblemTag leaked in "text" -> the suggestion is removed.
    [Fact]
    public async Task SuggestionExposingRawProblemTagInText_IsRemoved()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Defense", "Address OpponentHotFromThree immediately.", "Their three-point volume has been high."),
            ("Defense", "Contest every three-point attempt.", "The opponent has been hot from three."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.OpponentHotFromThree },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Contest every three-point attempt.", suggestion.Text);
    }

    // 3. Internal ProblemTag leaked in "reason" -> the suggestion is removed.
    [Fact]
    public async Task SuggestionExposingRawProblemTagInReason_IsRemoved()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Defense", "Close out harder on shooters.", "problemTag OpponentHotFromThree triggered."),
            ("Defense", "Contest every three-point attempt.", "The opponent has been hot from three."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.OpponentHotFromThree },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Contest every three-point attempt.", suggestion.Text);
    }

    // 4. Internal rules-profile name leaked -> the suggestion is removed.
    [Fact]
    public async Task SuggestionExposingRawRulesProfileName_IsRemoved()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Other", "Adjust rotations.", "Using Amateur_Default thresholds to flag this."),
            ("Other", "Keep rotations tight.", "This matches typical amateur-level expectations."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(rulesProfile: "Amateur_Default"),
            Array.Empty<ProblemTag>(),
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Keep rotations tight.", suggestion.Text);
    }

    // 5. Natural basketball language that merely resembles a tag must survive -
    // only an exact, verbatim identifier match is rejected.
    [Theory]
    [InlineData("The opponent has been hot from three.")]
    [InlineData("Improve perimeter defense against three-point shooting.")]
    public async Task NaturalBasketballLanguage_IsNotFiltered(string reason)
    {
        var body = ResponsesApiEnvelope.Build(("Defense", "Contest more threes.", reason));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.OpponentHotFromThree, ProblemTag.PerimeterDefenseProblem },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal(reason, suggestion.Reason);
    }

    // 6. Blank-suggestion filtering (from Milestone 0/1) still works alongside the
    // identifier filter.
    [Fact]
    public async Task BlankSuggestion_IsStillRemoved_AlongsideIdentifierFilter()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Defense", "   ", "   "),
            ("Defense", "Contest every three-point attempt.", "The opponent has been hot from three."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.OpponentHotFromThree },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Contest every three-point attempt.", suggestion.Text);
    }

    // 7. Mixed response: only the suggestion exposing an identifier is removed,
    // the clean one is preserved.
    [Fact]
    public async Task MixedResponse_RemovesOnlyTheSuggestionExposingAnIdentifier()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Defense", "OpponentHotFromThree", "Flagged by the rules engine."),
            ("Defense", "Contest every three-point attempt.", "The opponent has been hot from three."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.OpponentHotFromThree },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Contest every three-point attempt.", suggestion.Text);
    }
}
