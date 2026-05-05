namespace CoachHoopsAI.Admin.Models;

/// <summary>
/// Shape of one item inside <see cref="AnalysisRecordDto.SuggestionsJson"/>.
/// </summary>
public sealed class SuggestionDto
{
    public string Category { get; init; } = "Other";
    public string Text { get; init; } = "";
    public string Reason { get; init; } = "";
}
