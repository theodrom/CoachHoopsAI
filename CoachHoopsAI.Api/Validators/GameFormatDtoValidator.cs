using CoachHoopsAI.Api.Contracts;
using FluentValidation;

namespace CoachHoopsAI.Api.Validators
{
    public class GameFormatDtoValidator : AbstractValidator<GameFormatDto>
    {
        public GameFormatDtoValidator()
        {
            RuleFor(x => x.RegulationPeriods).GreaterThan(0);
            RuleFor(x => x.RegulationPeriodMinutes).GreaterThan(0);
            RuleFor(x => x.OvertimePeriodMinutes).GreaterThan(0);
            RuleFor(x => x.Name).MaximumLength(80);
        }
    }
}
