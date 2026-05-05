namespace CoachHoopsAI.Api.Contracts
{
    public class AnalyzeGameResponse
    {
        public Guid AnalysisId { get; set; }

        public List<string>? ProblemTags { get; set; }

        public SuggestionsResponse Suggestions { get; set; } = new SuggestionsResponse();

        // V1.1 diagnostics (optional but returned by default)
        public GameDiagnosticsDto Diagnostics { get; set; } = new GameDiagnosticsDto();
    }
}
