using System;
using System.Collections.Generic;

namespace CoachHoopsAI.Admin.Models
{
    public class AnalyzeGameResponseDto
    {
        public Guid AnalysisId { get; set; }
        public List<string>? ProblemTags { get; set; }
        public SuggestionsResponseDto Suggestions { get; set; } = new SuggestionsResponseDto();
        public GameDiagnosticsDto Diagnostics { get; set; } = new GameDiagnosticsDto();
    }

    public class SuggestionsResponseDto
    {
        public List<string> Suggestions { get; set; } = new();
    }

    public class GameDiagnosticsDto
    {
        public string? Summary { get; set; }
    }
}