using CoachHoopsAI.Admin.Models;

namespace CoachHoopsAI.Admin.Services;

public interface IAnalysisApiClient
{
    Task<PagedResultDto<AnalysisRecordListItemDto>> SearchAsync(
        AnalysisSearchQueryDto query,
        CancellationToken cancellationToken = default);

    Task<AnalysisRecordDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AnalyzeGameResponseDto> AnalyzeGameAsync(
        AnalyzeGameRequestDto request,
        CancellationToken cancellationToken = default);
}
