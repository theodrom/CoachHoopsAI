using CoachHoopsAI.Api.Models;

namespace CoachHoopsAI.Api.Contracts
{
    public class SuggestionsResponse
    {
        public IReadOnlyCollection<SuggestionItemDto>? Offense { get; set; }
        public IReadOnlyCollection<SuggestionItemDto>? Defense { get; set; }
        public IReadOnlyCollection<SuggestionItemDto>? Other { get; set; }
    }
}
