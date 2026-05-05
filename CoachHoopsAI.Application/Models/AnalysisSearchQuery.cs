using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachHoopsAI.Application.Models
{
    public class AnalysisSearchQuery
    {
        public string? TeamName { get; init; }
        public string? Level { get; init; }
        public string? Tag { get; init; }

        public DateTime? FromUtc { get; init; }
        public DateTime? ToUtc { get; init; }

        public int Page { get; init; } = 1;      // 1-based
        public int PageSize { get; init; } = 20; // cap in repo
    }
}
