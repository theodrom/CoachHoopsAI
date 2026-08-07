using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;

namespace CoachHoopsAI.Application.Models
{
    public class GameAnalysisResult
    {
        public Level Level { get; set;  }
        public IReadOnlyCollection<Domain.Enums.ProblemTag>? ProblemTags { get; set; }
        public IReadOnlyCollection<Suggestion>? Suggestions { get; set; }

        public GameDiagnostics? Diagnostics { get; set; }  // V1.1

        // The model identifier reported by whichever ILlmSuggestionClient produced
        // Suggestions above (see AnalysisHistoryService, which persists this).
        public string AiModel { get; set; } = string.Empty;

        public GameAnalysisResult() { }
            
    }
}
