using System.Net;
using System.Net.Http.Json;
using System.Text;
using CoachHoopsAI.Admin.Models;

namespace CoachHoopsAI.Admin.Services;

/// <summary>
/// Typed client over <see cref="HttpClient"/> that targets <c>CoachHoopsAI.Api</c>'s
/// analyses endpoints. Surfaces non-2xx responses as <see cref="ApiClientException"/>
/// so the UI can render them as readable error messages.
/// </summary>
public sealed class AnalysisApiClient : IAnalysisApiClient
{
    private readonly HttpClient _http;

    public AnalysisApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResultDto<AnalysisRecordListItemDto>> SearchAsync(
        AnalysisSearchQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var url = BuildSearchUrl(query);

        try
        {
            var result = await _http.GetFromJsonAsync<PagedResultDto<AnalysisRecordListItemDto>>(url, cancellationToken);
            return result ?? new PagedResultDto<AnalysisRecordListItemDto>();
        }
        catch (HttpRequestException ex)
        {
            throw new ApiClientException(
                $"Failed to load analyses from API ({ex.StatusCode?.ToString() ?? "no response"}): {ex.Message}", ex);
        }
    }

    public async Task<AnalysisRecordDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resp = await _http.GetAsync($"api/analyses/{id}", cancellationToken);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;

            resp.EnsureSuccessStatusCode();

            return await resp.Content.ReadFromJsonAsync<AnalysisRecordDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiClientException(
                $"Failed to load analysis {id} from API ({ex.StatusCode?.ToString() ?? "no response"}): {ex.Message}", ex);
        }
    }

    private static string BuildSearchUrl(AnalysisSearchQueryDto q)
    {
        var sb = new StringBuilder("api/analyses?");
        sb.Append("page=").Append(q.Page);
        sb.Append("&pageSize=").Append(q.PageSize);

        if (!string.IsNullOrWhiteSpace(q.TeamName))
            sb.Append("&teamName=").Append(Uri.EscapeDataString(q.TeamName));
        if (!string.IsNullOrWhiteSpace(q.Level))
            sb.Append("&level=").Append(Uri.EscapeDataString(q.Level));
        if (!string.IsNullOrWhiteSpace(q.Tag))
            sb.Append("&tag=").Append(Uri.EscapeDataString(q.Tag));
        if (q.FromUtc.HasValue)
            sb.Append("&fromUtc=").Append(Uri.EscapeDataString(q.FromUtc.Value.ToString("o")));
        if (q.ToUtc.HasValue)
            sb.Append("&toUtc=").Append(Uri.EscapeDataString(q.ToUtc.Value.ToString("o")));

        return sb.ToString();
    }
}

public sealed class ApiClientException : Exception
{
    public ApiClientException(string message, Exception? inner = null) : base(message, inner) { }
}
