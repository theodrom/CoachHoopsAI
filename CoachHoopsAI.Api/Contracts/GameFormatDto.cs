namespace CoachHoopsAI.Api.Contracts
{
    public class GameFormatDto
    {
        public int RegulationPeriods { get; set; }
        public int RegulationPeriodMinutes { get; set; }
        public int OvertimePeriodMinutes { get; set; }

        // Descriptive only, e.g. "FIBA", "NBA", "EasyBasket 8x4". Never drives calculations.
        public string? Name { get; set; }
    }
}
