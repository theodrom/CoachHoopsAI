using CoachHoopsAI.Api.Contracts;
using FluentValidation;

namespace CoachHoopsAI.Api.Validators
{
    public class TeamStatsDtoValidator : AbstractValidator<TeamStatsDto>
    {
        public TeamStatsDtoValidator()
        {
            RuleFor(x => x.Points).GreaterThanOrEqualTo(0);

            RuleFor(x => x.FieldGoalsMade).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FieldGoalsAttempted).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ThreePointsMade).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ThreePointsAttempted).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FreeThrowsMade).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FreeThrowsAttempted).GreaterThanOrEqualTo(0);
            RuleFor(x => x.OffensiveRebounds).GreaterThanOrEqualTo(0);
            RuleFor(x => x.DefensiveRebounds).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Assists).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Turnovers).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Steals).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Blocks).GreaterThanOrEqualTo(0);
            RuleFor(x => x.PersonalFouls).GreaterThanOrEqualTo(0);

            RuleFor(x => x.FieldGoalsMade)
                .LessThanOrEqualTo(x => x.FieldGoalsAttempted)
                .WithMessage("FieldGoalsMade cannot exceed FieldGoalsAttempted.");

            RuleFor(x => x.ThreePointsMade)
                .LessThanOrEqualTo(x => x.ThreePointsAttempted)
                .WithMessage("ThreePointsMade cannot exceed ThreePointsAttempted.");

            RuleFor(x => x.FreeThrowsMade)
                .LessThanOrEqualTo(x => x.FreeThrowsAttempted)
                .WithMessage("FreeThrowsMade cannot exceed FreeThrowsAttempted.");

            // Three-pointers are a subset of field goals.
            RuleFor(x => x.ThreePointsMade)
                .LessThanOrEqualTo(x => x.FieldGoalsMade)
                .WithMessage("ThreePointsMade cannot exceed FieldGoalsMade.");

            RuleFor(x => x.ThreePointsAttempted)
                .LessThanOrEqualTo(x => x.FieldGoalsAttempted)
                .WithMessage("ThreePointsAttempted cannot exceed FieldGoalsAttempted.");
        }
    }
}
