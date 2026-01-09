using CoachHoopsAI.Domain.Enums;

namespace CoachHoopsAI.Domain.Entities
{
    public class Game
    {
        public Guid Id { get; }
        public DateTime? Date { get; }

        public Level Level { get; }

        public TeamStats Team { get; }
        public TeamStats Opponent { get; }

        public string Notes { get; }

        public Game(
            Guid id,
            Level level,
            TeamStats team,
            TeamStats opponent,
            string notes,
            DateTime? date = null)
        {
            Id = id;
            Level = level;
            Team = team ?? throw new ArgumentNullException(nameof(team));
            Opponent = opponent ?? throw new ArgumentNullException(nameof(opponent));
            Notes = notes ?? string.Empty;
            Date = date;
        }
    }
}
