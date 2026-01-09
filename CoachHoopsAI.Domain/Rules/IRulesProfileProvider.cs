using CoachHoopsAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachHoopsAI.Domain.Rules
{
    public interface IRulesProfileProvider
    {
        ResolvedRulesProfile Resolve(Level level, string? overrideProfileName);
    }
}
