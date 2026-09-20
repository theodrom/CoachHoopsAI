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

    // 8. TooManyThreePointAttempts is the one tag whose bare enum name reads as a
    // resolved verdict ("too many") rather than a neutral description, so the
    // prompt substitutes a neutral phrase for it - proven here by inspecting the
    // actual outgoing request, not just the response-filtering behavior the other
    // tests cover.
    [Fact]
    public async Task PromptForTooManyThreePointAttempts_SendsNeutralDescription_NotTheRawEnumName()
    {
        // Canned reason deliberately avoids both the neutral phrase and the raw
        // enum name, so neither of tests 9/10's concerns interferes with this
        // test observing the outgoing request.
        var body = ResponsesApiEnvelope.Build(("Offense", "Diversify shot selection.", "Shot selection could be more balanced."));
        var capturingHandler = new FakeResponsesApiHandler(body);
        var client = new OpenAiSuggestionClientHttp(
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions { ApiKey = "test-key", Model = "test-model", BaseUrl = "http://localhost" }),
            new HttpClient(capturingHandler));

        await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.TooManyThreePointAttempts },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        Assert.NotNull(capturingHandler.LastRequestBody);
        Assert.Contains("High three-point share with low three-point percentage", capturingHandler.LastRequestBody);
        Assert.DoesNotContain("TooManyThreePointAttempts", capturingHandler.LastRequestBody);
    }

    // 9. The neutral phrase is the sanctioned coaching-language description of
    // this finding, not an internal identifier - a suggestion using it verbatim
    // must be KEPT, not treated as a leak. (An earlier version of this filter
    // checked the transmitted description instead of the raw enum name and would
    // have wrongly discarded this suggestion; that approach was reverted.)
    [Fact]
    public async Task SuggestionUsingNeutralDescriptionForTooManyThreePointAttempts_IsAllowed()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Offense", "Work more two-point looks into the offense.",
             "High three-point share with low three-point percentage."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.TooManyThreePointAttempts },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Work more two-point looks into the offense.", suggestion.Text);
        Assert.Equal("High three-point share with low three-point percentage.", suggestion.Reason);
    }

    // 10. The raw enum name must still be rejected if the model produces it, even
    // though this request's prompt never showed the model that string for this
    // tag (test 8 proves it wasn't sent). The filter checks a static, code-shaped
    // identifier, independent of what was actually transmitted.
    [Fact]
    public async Task SuggestionExposingRawEnumNameForTooManyThreePointAttempts_IsRemoved()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Offense", "Address TooManyThreePointAttempts immediately.", "Flagged by the rules engine."),
            ("Offense", "Work more two-point looks into the offense.", "Three-point shooting has been inefficient at high volume."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.TooManyThreePointAttempts },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Work more two-point looks into the offense.", suggestion.Text);
    }

    // 11. OurShootingInefficiency is the second tag whose bare enum name reads as a
    // broader, overclaiming verdict ("shooting" generally) than the trigger it
    // names (three-point percentage specifically), so the prompt substitutes a
    // neutral phrase for it too - same pattern as TooManyThreePointAttempts above,
    // proven here by inspecting the actual outgoing request.
    [Fact]
    public async Task PromptForOurShootingInefficiency_SendsNeutralDescription_NotTheRawEnumName()
    {
        // Canned reason deliberately avoids both the neutral phrase and the raw
        // enum name, so neither of tests 12/13's concerns interferes with this
        // test observing the outgoing request.
        var body = ResponsesApiEnvelope.Build(("Offense", "Get more reps up from the corners.", "Accuracy from deep has lagged this game."));
        var capturingHandler = new FakeResponsesApiHandler(body);
        var client = new OpenAiSuggestionClientHttp(
            Microsoft.Extensions.Options.Options.Create(new OpenAiOptions { ApiKey = "test-key", Model = "test-model", BaseUrl = "http://localhost" }),
            new HttpClient(capturingHandler));

        await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.OurShootingInefficiency },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        Assert.NotNull(capturingHandler.LastRequestBody);
        Assert.Contains("Low three-point percentage", capturingHandler.LastRequestBody);
        Assert.DoesNotContain("OurShootingInefficiency", capturingHandler.LastRequestBody);
    }

    // 12. The neutral phrase is the sanctioned coaching-language description of
    // this finding, not an internal identifier - a suggestion using it verbatim
    // must be KEPT, not treated as a leak.
    [Fact]
    public async Task SuggestionUsingNeutralDescriptionForOurShootingInefficiency_IsAllowed()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Offense", "Run more actions that get shooters open looks from deep.",
             "Low three-point percentage."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.OurShootingInefficiency },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Run more actions that get shooters open looks from deep.", suggestion.Text);
        Assert.Equal("Low three-point percentage.", suggestion.Reason);
    }

    // 13. The raw enum name must still be rejected if the model produces it, even
    // though this request's prompt never showed the model that string for this
    // tag (test 11 proves it wasn't sent).
    [Fact]
    public async Task SuggestionExposingRawEnumNameForOurShootingInefficiency_IsRemoved()
    {
        var body = ResponsesApiEnvelope.Build(
            ("Offense", "Address OurShootingInefficiency immediately.", "Flagged by the rules engine."),
            ("Offense", "Run more actions that get shooters open looks from deep.", "Three-point shooting has been inefficient this game."));
        var client = CreateClient(body);

        var result = await client.GetSuggestionsAsync(
            CreateInput(),
            new[] { ProblemTag.OurShootingInefficiency },
            CreateDiagnostics("Amateur_Default"),
            "Amateur_Default");

        var suggestion = Assert.Single(result);
        Assert.Equal("Run more actions that get shooters open looks from deep.", suggestion.Text);
    }
}
