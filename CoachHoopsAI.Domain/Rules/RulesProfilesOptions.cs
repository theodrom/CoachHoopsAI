using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachHoopsAI.Domain.Rules
{
    public class RulesProfilesOptions
    {
        public Dictionary<string, RulesProfile> Profiles { get; set; } = new();
        public Dictionary<string, string> DefaultProfileByLevel { get; set; } = new();
    }
}
