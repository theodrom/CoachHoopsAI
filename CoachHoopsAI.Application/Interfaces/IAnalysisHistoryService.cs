using CoachHoopsAI.Application.Models;

namespace CoachHoopsAI.Application.Interfaces
{
    public interface IAnalysisHistoryService
    {
        Task<(GameAnalysisResult? Result, Guid AnalysisId)> AnalyzeAndStoreAsync(GameAnalysisInput input);
    }
}
