using CoachHoopsAI.Application.Models;

namespace CoachHoopsAI.Application.Interfaces
{
    public interface IGameAnalysisService
    {
        Task<GameAnalysisResult> AnalyzeAsync(GameAnalysisInput input);
    }
}
