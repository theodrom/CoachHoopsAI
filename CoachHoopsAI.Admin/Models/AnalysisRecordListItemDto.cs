namespace CoachHoopsAI.Admin.Models;

/// <summary>
/// Mirrors <c>CoachHoopsAI.Application.Models.AnalysisRecordListItem</c> as exposed by
/// <c>GET /api/analyses</c>. Frontend-owned copy: never reference Application/Persistence types.
/// </summary>
public sealed class AnalysisRecordListItemDto
{
    public Guid Id { get; init; }
    public DateTime CreatedUtc { get; init; }

    public string Level { get; init; } = "";
    public string AppliedRulesProfile { get; init; } = "";

    public DateTime? GameDate { get; init; }
    public string? TeamName { get; init; }
    public string? OpponentName { get; init; }
}
