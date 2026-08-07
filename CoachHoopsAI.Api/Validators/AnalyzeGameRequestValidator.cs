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

        public AnalyzeGameRequestValidator(
            IValidator<TeamStatsDto> teamStatsValidator,
            IValidator<GameFormatDto> gameFormatValidator,
            IValidator<GameTimingDto> gameTimingValidator)
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

            RuleFor(x => x.GameFormat)
                .NotNull().WithMessage("'GameFormat' must be provided.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.GameFormat!).SetValidator(gameFormatValidator);
                });

            RuleFor(x => x.GameTiming)
                .NotNull().WithMessage("'GameTiming' must be provided.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.GameTiming!).SetValidator(gameTimingValidator);
                });

            // Cross-field: the clock can't show more time than the period it's in
            // actually runs for. Needs GameFormat and GameTiming together, so it
            // can't live on either DTO's own validator.
            RuleFor(x => x)
                .Custom((request, context) =>
                {
                    var format = request.GameFormat;
                    var timing = request.GameTiming;
                    if (format == null || timing == null)
                        return;

                    var isOvertime = timing.CurrentPeriod > format.RegulationPeriods;
                    var applicablePeriodMinutes = isOvertime ? format.OvertimePeriodMinutes : format.RegulationPeriodMinutes;
                    var maxSeconds = applicablePeriodMinutes * 60;

                    if (timing.ClockRemainingSeconds > maxSeconds)
                    {
                        context.AddFailure(
                            $"{nameof(request.GameTiming)}.{nameof(timing.ClockRemainingSeconds)}",
                            $"ClockRemainingSeconds ({timing.ClockRemainingSeconds}) cannot exceed the " +
                            $"{(isOvertime ? "overtime" : "regulation")} period length ({maxSeconds} seconds).");
                    }
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
