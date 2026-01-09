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

        public GameAnalysisResult() { }
            
    }
}
