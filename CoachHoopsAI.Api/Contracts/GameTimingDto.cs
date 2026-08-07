namespace CoachHoopsAI.Api.Contracts
{
    public class GameTimingDto
    {
        public int CurrentPeriod { get; set; }

        // Clock remaining in the current period, expressed as whole seconds rather
        // than a serialized TimeSpan - explicit and unambiguous over the wire, and
        // trivial for a plain numeric form input on the Admin side.
        public int ClockRemainingSeconds { get; set; }
    }
}
