using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;

namespace CoachHoopsAI.Application.Models
{
    public class GameAnalysisInput(
        Level level,
        TeamStats team,
        TeamStats opponent,
        string notes,
        GameMetadata? metadata = null,
        string? rulesProfile = null)
    {
        public Level Level { get; } = level;
        public TeamStats Team { get; } = team;
        public TeamStats Opponent { get; } = opponent;
        public string Notes { get; } = notes ?? string.Empty;
        public GameMetadata? Metadata { get; } = metadata; // V1.1
        public string? RulesProfile { get; } = rulesProfile;  // V1.3
    }
}
