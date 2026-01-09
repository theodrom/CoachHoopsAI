using CoachHoopsAI.Api.Contracts;
using FluentValidation;

namespace CoachHoopsAI.Api.Validators
{
    public class AnalyzeGameRequestValidator : AbstractValidator<AnalyzeGameRequest>
    {
        private static readonly string[] AllowedLevels =
        {
            "EasyBasket", "Youth", "Amateur", "Pro"
        };

        public AnalyzeGameRequestValidator(IValidator<TeamStatsDto> teamStatsValidator)
        {
            RuleFor(x => x.Team)
                .NotNull().WithMessage("'Team' must be provided.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.Team!).SetValidator(teamStatsValidator);
                });

            RuleFor(x => x.Opponent)
                .NotNull().WithMessage("'Opponent' must be provided.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.Opponent!).SetValidator(teamStatsValidator);
                });

            RuleFor(x => x.Level)
                .NotEmpty()
                .Must(BeAValidLevel)
                .WithMessage($"Level must be one of: {string.Join(", ", AllowedLevels)}.");

            RuleFor(x => x.Notes)
                .MaximumLength(2000);

            RuleFor(x => x.TeamName)
                .MaximumLength(80);

            RuleFor(x => x.OpponentName)
                .MaximumLength(80);

            RuleFor(x => x.Competition)
                .MaximumLength(120);

            RuleFor(x => x.Season)
                .MaximumLength(20);

            RuleFor(x => x.Location)
                .MaximumLength(120);

            RuleFor(x => x.GameDate)
                .Must(d => d == null || d.Value.Year >= 1900)
                .WithMessage("GameDate must be a valid date.");

            RuleFor(x => x.RulesProfile)
                .MaximumLength(80);
        }

        private static bool BeAValidLevel(string? level)
        {
            if (string.IsNullOrWhiteSpace(level)) return false;

            var normalized = level.Replace(" ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("_", "", StringComparison.OrdinalIgnoreCase)
                .Replace("-", "", StringComparison.OrdinalIgnoreCase);

            return normalized.Equals("easybasket", StringComparison.OrdinalIgnoreCase)
                   || normalized.Equals("youth", StringComparison.OrdinalIgnoreCase)
                   || normalized.Equals("amateur", StringComparison.OrdinalIgnoreCase)
                   || normalized.Equals("pro", StringComparison.OrdinalIgnoreCase);
        }
    }
}
