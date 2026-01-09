using CoachHoopsAI.Api.Contracts;
using CoachHoopsAI.Api.Validators;
using CoachHoopsAI.Application.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;


namespace CoachHoopsAI.Api.Controllers
{
    [ApiController]
    [Route("api/game-analysis")]
    public class GameAnalysisController(IGameAnalysisService gameAnalysisService, IValidator<AnalyzeGameRequest> validator) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<AnalyzeGameResponse>> AnalyzeGame([FromBody] AnalyzeGameRequest request)
        {
            ValidationResult results = await validator.ValidateAsync(request);

            if (!results.IsValid)
            {
                return ValidationProblem(new ValidationProblemDetails(FluentValidationExtensions.ToProblemDetailsErrors(results))
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext?.Request?.Path.Value
                });
            }

            var input = request.ToGameAnalysisInput();
            var result = await gameAnalysisService.AnalyzeAsync(input);
            return Ok(result.ToResponseDto());
        }
    }
}
