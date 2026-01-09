using CoachHoopsAI.Domain.Enums;

namespace CoachHoopsAI.Domain.Entities
{
    public class Suggestion
    {
        public SuggestionCategory Category { get; }
        public string Text { get; }
        public string Reason { get; }

        public Suggestion(
            SuggestionCategory category,
            string text,
            string reason)
        {
            Category = category;
            Text = text;
            Reason = reason;
        }
    }
}
