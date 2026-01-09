

namespace CoachHoopsAI.Application.Models
{
    public class GameMetadata
    {
        public DateTime? GameDate { get; }
        public string? TeamName { get; }
        public string? OpponentName { get; }
        public string? Competition { get; }
        public string? Season { get; }
        public string? Location { get; }

        public GameMetadata(
            DateTime? gameDate,
            string? teamName,
            string? opponentName,
            string? competition,
            string? season,
            string? location)
        {
            GameDate = gameDate;
            TeamName = teamName;
            OpponentName = opponentName;
            Competition = competition;
            Season = season;
            Location = location;
        }
    }
}
