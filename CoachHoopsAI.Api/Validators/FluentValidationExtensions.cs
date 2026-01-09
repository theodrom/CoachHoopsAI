using FluentValidation.Results;

namespace CoachHoopsAI.Api.Validators
{
    public static class FluentValidationExtensions
    {
        public static IDictionary<string, string[]> ToProblemDetailsErrors(ValidationResult result)
        {
            return result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).Distinct().ToArray()
                );
        }
    }
}
