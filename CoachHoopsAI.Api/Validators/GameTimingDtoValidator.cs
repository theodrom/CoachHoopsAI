using CoachHoopsAI.Api.Contracts;
using FluentValidation;

namespace CoachHoopsAI.Api.Validators
{
    // Covers the invariants that only need GameTiming itself. The clock-vs-period-length
    // check needs GameFormat as well (a regulation clock is capped by
    // RegulationPeriodMinutes, an overtime clock by OvertimePeriodMinutes), so that
    // cross-field rule lives on AnalyzeGameRequestValidator where both are available.
    public class GameTimingDtoValidator : AbstractValidator<GameTimingDto>
    {
        public GameTimingDtoValidator()
        {
            RuleFor(x => x.CurrentPeriod).GreaterThanOrEqualTo(1);
            RuleFor(x => x.ClockRemainingSeconds).GreaterThanOrEqualTo(0);
        }
    }
}
