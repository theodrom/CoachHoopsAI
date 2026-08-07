using System.ComponentModel.DataAnnotations;

namespace CoachHoopsAI.Admin.Models
{
    public class GameTimingDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Current period must be at least 1.")]
        public int CurrentPeriod { get; set; } = 1;

        [Range(0, int.MaxValue, ErrorMessage = "Clock remaining cannot be negative.")]
        public int ClockRemainingSeconds { get; set; } = 600;
    }
}
