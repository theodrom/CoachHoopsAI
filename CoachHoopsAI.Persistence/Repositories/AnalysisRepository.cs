using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachHoopsAI.Persistence.Repositories
{
    public sealed class AnalysisRepository : IAnalysisRepository
    {
        private readonly CoachHoopsAIDbContext _db;

        public AnalysisRepository(CoachHoopsAIDbContext db) => _db = db;

        public async Task<Guid> SaveAsync(AnalysisRecord record)
        {
            var entity = new AnalysisRecordEntity
            {
                Id = record.Id,
                CreatedUtc = record.CreatedUtc,

                Level = record.Level,
                RequestedRulesProfile = record.RequestedRulesProfile,
                AppliedRulesProfile = record.AppliedRulesProfile,

                GameDate = record.GameDate,
                TeamName = record.TeamName,
                OpponentName = record.OpponentName,
                Season = record.Season,
                Location = record.Location,

                RulesetVersion = record.RulesetVersion,
                PromptVersion = record.PromptVersion,
                AiModel = record.AiModel,

                InputJson = record.InputJson,
                ProblemTagsJson = record.ProblemTagsJson,
                DiagnosticsJson = record.DiagnosticsJson,
                SuggestionsJson = record.SuggestionsJson
            };

            _db.Analyses.Add(entity);
            await _db.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<AnalysisRecord?> GetByIdAsync(Guid id)
        {
            var e = await _db.Analyses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (e == null) return null;

            return new AnalysisRecord
            {
                Id = e.Id,
                CreatedUtc = e.CreatedUtc,
                Level = e.Level,
                RequestedRulesProfile = e.RequestedRulesProfile,
                AppliedRulesProfile = e.AppliedRulesProfile,
                GameDate = e.GameDate,
                TeamName = e.TeamName,
                OpponentName = e.OpponentName,
                Season = e.Season,
                Location = e.Location,
                RulesetVersion = e.RulesetVersion,
                PromptVersion = e.PromptVersion,
                AiModel = e.AiModel,
                InputJson = e.InputJson,
                ProblemTagsJson = e.ProblemTagsJson,
                DiagnosticsJson = e.DiagnosticsJson,
                SuggestionsJson = e.SuggestionsJson
            };
        }

        public async Task<PagedResult<AnalysisRecordListItem>> SearchAsync(AnalysisSearchQuery query)
        {
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var page = query.Page < 1 ? 1 : query.Page;

            IQueryable<AnalysisRecordEntity> q = _db.Analyses.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.TeamName))
                q = q.Where(x => x.TeamName != null && x.TeamName.Contains(query.TeamName));

            if (!string.IsNullOrWhiteSpace(query.Level))
                q = q.Where(x => x.Level == query.Level);

            if (query.FromUtc != null)
                q = q.Where(x => x.CreatedUtc >= query.FromUtc);

            if (query.ToUtc != null)
                q = q.Where(x => x.CreatedUtc <= query.ToUtc);

            // Tag filtering: simplest possible approach for V3.0
            // We store tags as JSON array; for SQLite, use Contains() on the JSON string.
            if (!string.IsNullOrWhiteSpace(query.Tag))
                q = q.Where(x => x.ProblemTagsJson.Contains(query.Tag));

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(x => x.CreatedUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AnalysisRecordListItem
                {
                    Id = x.Id,
                    CreatedUtc = x.CreatedUtc,
                    Level = x.Level,
                    AppliedRulesProfile = x.AppliedRulesProfile,
                    GameDate = x.GameDate,
                    TeamName = x.TeamName,
                    OpponentName = x.OpponentName,
                    Season = x.Season,
                    Location = x.Location,
                    ProblemTagsJson = x.ProblemTagsJson
                })
                .ToListAsync();

            return new PagedResult<AnalysisRecordListItem>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
        }
    }
}
