using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;

namespace CoachHoopsAI.Application.Tests.TestDoubles;

// Records the AnalysisRecord AnalysisHistoryService actually hands to SaveAsync,
// so persistence tests can assert on exactly what would have been written to the
// database without needing a real one.
public class FakeAnalysisRepository : IAnalysisRepository
{
    public Guid IdToReturn { get; set; } = Guid.NewGuid();

    public AnalysisRecord? LastSavedRecord { get; private set; }
    public int SaveCallCount { get; private set; }

    public Task<Guid> SaveAsync(AnalysisRecord record)
    {
        SaveCallCount++;
        LastSavedRecord = record;
        return Task.FromResult(IdToReturn);
    }

    public Task<AnalysisRecord?> GetByIdAsync(Guid id) => Task.FromResult<AnalysisRecord?>(null);

    public Task<PagedResult<AnalysisRecordListItem>> SearchAsync(AnalysisSearchQuery query) =>
        Task.FromResult(new PagedResult<AnalysisRecordListItem>());
}
