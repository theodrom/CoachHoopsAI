using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using CoachHoopsAI.Domain.GameContext;

namespace CoachHoopsAI.Application.Models
{
    public class GameAnalysisInput(
        Level level,
        TeamStats team,
        TeamStats opponent,
        string notes,
        GameMetadata? metadata = null,
        string? rulesProfile = null,
        GameFormat? gameFormat = null,
        GameTiming? gameTiming = null)
    {
        public Level Level { get; } = level;
        public TeamStats Team { get; } = team;
        public TeamStats Opponent { get; } = opponent;
        public string Notes { get; } = notes ?? string.Empty;
        public GameMetadata? Metadata { get; } = metadata; // V1.1
        public string? RulesProfile { get; } = rulesProfile;  // V1.3

        // V1.4 (Milestone 1): the format and current position of this specific game.
        // Not yet consumed by the rules engine, diagnostics, or the LLM prompt -
        // stored as facts for later milestones to build calculated analytics on top of.
        public GameFormat? GameFormat { get; } = gameFormat;
        public GameTiming? GameTiming { get; } = gameTiming;
    }
}
