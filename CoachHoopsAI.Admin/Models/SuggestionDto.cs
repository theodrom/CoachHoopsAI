namespace CoachHoopsAI.Admin.Models;

/// <summary>
/// Shape of one item inside <see cref="AnalysisRecordDto.SuggestionsJson"/>.
/// </summary>
using System.Text.Json.Serialization;

public sealed class SuggestionDto
{
    [JsonIgnore]
    public string Category { get; private set; } = "Other";

    [JsonPropertyName("category")]
    public int CategoryValue
    {
        set { Category = MapCategory(value); }
    }

    [JsonPropertyName("text")]
    public string Text { get; init; } = "";

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "";

    private static string MapCategory(int value)
        => value switch
        {
            0 => "Offense",
            1 => "Defense",
            2 => "Other",
            _ => $"Unknown({value})"
        };
}
