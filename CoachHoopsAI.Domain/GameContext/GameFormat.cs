namespace CoachHoopsAI.Domain.GameContext
{
    // The structure of a specific game: how many regulation periods it has, how long
    // each one runs, and how long an overtime period runs. Deliberately generic -
    // NBA/FIBA/EasyBasket are just numeric presets a caller can choose, not concepts
    // the domain hardcodes. Examples: 4x12, 4x10, 4x5, 8x4.
    public sealed record GameFormat
    {
        public int RegulationPeriods { get; init; }
        public int RegulationPeriodMinutes { get; init; }
        public int OvertimePeriodMinutes { get; init; }

        // Descriptive only (e.g. "FIBA", "NBA", "EasyBasket 8x4"). Must never drive
        // calculations - the numeric structure above is the only thing the domain reads.
        public string? Name { get; init; }

        public TimeSpan RegulationDuration => TimeSpan.FromMinutes((double)RegulationPeriods * RegulationPeriodMinutes);
    }
}
