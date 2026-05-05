namespace CoachHoopsAI.Admin.Models;

/// <summary>
/// Generic paged envelope returned by the API search endpoints.
/// </summary>
public sealed class PagedResultDto<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();
}
