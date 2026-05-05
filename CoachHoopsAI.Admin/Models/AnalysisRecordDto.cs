namespace CoachHoopsAI.Admin.Models;

/// <summary>
/// Mirrors <c>CoachHoopsAI.Application.Models.AnalysisRecord</c> as exposed by
/// <c>GET /api/analyses/{id}</c>. The four *Json fields are raw JSON snapshots
/// stored at analysis time.
/// </summary>
public sealed class AnalysisRecordDto
{
    public Guid Id { get; init; }
    public DateTime CreatedUtc { get; init; }

    public string Level { get; init; } = "";
    public string? RequestedRulesProfile { get; init; }
    public string AppliedRulesProfile { get; init; } = "";

    public DateTime? GameDate { get; init; }
    public string? TeamName { get; init; }
    public string? OpponentName { get; init; }
    public string? Season { get; init; }
    public string? Location { get; init; }

    public string RulesetVersion { get; init; } = "";
    public string PromptVersion { get; init; } = "";
    public string AiModel { get; init; } = "";

    public string InputJson { get; init; } = "";
    public string ProblemTagsJson { get; init; } = "";
    public string DiagnosticsJson { get; init; } = "";
    public string SuggestionsJson { get; init; } = "";
}
