using CoachHoopsAI.Application.Models;

namespace CoachHoopsAI.Application.Interfaces
{
    public interface IAnalysisRepository
    {
        Task<Guid> SaveAsync(AnalysisRecord record);
        Task<AnalysisRecord?> GetByIdAsync(Guid id);
        Task<PagedResult<AnalysisRecordListItem>> SearchAsync(AnalysisSearchQuery query);
    }
}
