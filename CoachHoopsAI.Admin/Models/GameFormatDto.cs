using System.ComponentModel.DataAnnotations;

namespace CoachHoopsAI.Admin.Models
{
    public class GameFormatDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Regulation periods must be at least 1.")]
        public int RegulationPeriods { get; set; } = 4;

        [Range(1, int.MaxValue, ErrorMessage = "Regulation period length must be at least 1 minute.")]
        public int RegulationPeriodMinutes { get; set; } = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Overtime period length must be at least 1 minute.")]
        public int OvertimePeriodMinutes { get; set; } = 5;

        public string? Name { get; set; }
    }
}
