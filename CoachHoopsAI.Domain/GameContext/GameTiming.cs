namespace CoachHoopsAI.Domain.GameContext
{
    // Where the game currently is: a sequential period number and the clock remaining
    // in that period. Sequential numbering (1..RegulationPeriods = regulation,
    // RegulationPeriods+1.. = OT1, OT2, ...) means overtime state is always derivable
    // from GameFormat + CurrentPeriod - there is no separate "IsOvertime" input flag
    // that could contradict CurrentPeriod.
    public sealed record GameTiming
    {
        public int CurrentPeriod { get; init; }
        public TimeSpan ClockRemaining { get; init; }

        public bool IsOvertime(GameFormat format) => CurrentPeriod > format.RegulationPeriods;

        public int OvertimeNumber(GameFormat format) =>
            IsOvertime(format) ? CurrentPeriod - format.RegulationPeriods : 0;

        // Total game time played so far: completed periods at their full length, plus
        // the elapsed portion of the current period. Regulation periods and overtime
        // periods can have different lengths, so which length applies depends on
        // whether the current period is regulation or overtime.
        public TimeSpan ElapsedGameTime(GameFormat format)
        {
            if (!IsOvertime(format))
            {
                var periodLength = TimeSpan.FromMinutes(format.RegulationPeriodMinutes);
                var completedPeriods = CurrentPeriod - 1;
                var elapsedInCurrentPeriod = periodLength - ClockRemaining;
                return (periodLength * completedPeriods) + elapsedInCurrentPeriod;
            }

            var otPeriodLength = TimeSpan.FromMinutes(format.OvertimePeriodMinutes);
            var completedOvertimePeriods = OvertimeNumber(format) - 1;
            var elapsedInCurrentOvertime = otPeriodLength - ClockRemaining;
            return format.RegulationDuration + (otPeriodLength * completedOvertimePeriods) + elapsedInCurrentOvertime;
        }

        // Progress through regulation only, clamped to [0, 1]. Once overtime starts
        // there is no meaningful "percentage" of overtime to report - elapsed game
        // time is the more useful figure at that point - so this simply reports 1.0.
        public double RegulationProgress(GameFormat format)
        {
            if (IsOvertime(format))
                return 1.0;

            var regulationDuration = format.RegulationDuration;
            if (regulationDuration <= TimeSpan.Zero)
                return 0.0;

            var progress = ElapsedGameTime(format).TotalMinutes / regulationDuration.TotalMinutes;
            return Math.Clamp(progress, 0.0, 1.0);
        }
    }
}
