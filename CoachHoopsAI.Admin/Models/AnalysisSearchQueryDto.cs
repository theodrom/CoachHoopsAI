namespace CoachHoopsAI.Admin.Models;

/// <summary>
/// Optional filter parameters for <c>GET /api/analyses</c>.
/// </summary>
public sealed class AnalysisSearchQueryDto
{
    public string? TeamName { get; init; }
    public string? Level { get; init; }
    public string? Tag { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
