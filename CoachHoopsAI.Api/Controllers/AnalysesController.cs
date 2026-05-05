using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace CoachHoopsAI.Api.Controllers
{
    [ApiController]
    [Route("api/analyses")]
    public class AnalysesController(IAnalysisRepository repo) : ControllerBase
    {
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AnalysisRecord>> GetById(Guid id)
        {
            var record = await repo.GetByIdAsync(id);
            if (record == null) return NotFound();
            return Ok(record);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<AnalysisRecordListItem>>> Search(
            [FromQuery] string? teamName,
            [FromQuery] string? level,
            [FromQuery] string? tag,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await repo.SearchAsync(new AnalysisSearchQuery
            {
                TeamName = teamName,
                Level = level,
                Tag = tag,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Page = page,
                PageSize = pageSize
            });

            return Ok(result);
        }
    }
}
