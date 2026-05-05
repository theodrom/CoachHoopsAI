using CoachHoopsAI.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoachHoopsAI.Persistence
{
    public sealed class CoachHoopsAIDbContext : DbContext
    {
        public CoachHoopsAIDbContext(DbContextOptions<CoachHoopsAIDbContext> options) : base(options) { }

        public DbSet<AnalysisRecordEntity> Analyses => Set<AnalysisRecordEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var e = modelBuilder.Entity<AnalysisRecordEntity>();

            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CreatedUtc);
            e.HasIndex(x => x.TeamName);
            e.HasIndex(x => x.GameDate);

            e.Property(x => x.Level).IsRequired().HasMaxLength(32);
            e.Property(x => x.AppliedRulesProfile).IsRequired().HasMaxLength(128);
            e.Property(x => x.RequestedRulesProfile).HasMaxLength(128);

            e.Property(x => x.TeamName).HasMaxLength(128);
            e.Property(x => x.OpponentName).HasMaxLength(128);

            e.Property(x => x.RulesetVersion).IsRequired().HasMaxLength(32);
            e.Property(x => x.PromptVersion).IsRequired().HasMaxLength(32);
            e.Property(x => x.AiModel).IsRequired().HasMaxLength(64);

            // Snapshots as TEXT
            e.Property(x => x.InputJson).IsRequired();
            e.Property(x => x.ProblemTagsJson).IsRequired();
            e.Property(x => x.DiagnosticsJson).IsRequired();
            e.Property(x => x.SuggestionsJson).IsRequired();
        }
    }
}
