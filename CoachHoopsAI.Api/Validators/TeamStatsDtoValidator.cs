using CoachHoopsAI.Api.Contracts;
using FluentValidation;

namespace CoachHoopsAI.Api.Validators
{
    public class TeamStatsDtoValidator : AbstractValidator<TeamStatsDto>
    {
        public TeamStatsDtoValidator()
        {
            RuleFor(x => x.Points)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.FieldGoalPercentage)
                .InclusiveBetween(0.0, 1.0);

            RuleFor(x => x.ThreePointPercentage)
                .InclusiveBetween(0.0, 1.0);

            RuleFor(x => x.ThreePointAttempts)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.OffensiveRebounds)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.DefensiveRebounds)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Turnovers)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Fouls)
                .GreaterThanOrEqualTo(0);

            // Optional sanity checks you can enable later:
            // - If ThreePointAttempts == 0 then ThreePointPercentage should be 0
            RuleFor(x => x.ThreePointPercentage)
                .Must((stats, pct) => stats.ThreePointAttempts > 0 || pct == 0.0)
                .WithMessage("ThreePointPercentage must be 0 when ThreePointAttempts is 0.");
        }
    }
}
