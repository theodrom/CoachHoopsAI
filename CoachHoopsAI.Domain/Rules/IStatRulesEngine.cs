using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Domain.Rules
{
    public interface IStatRulesEngine
    {
        IReadOnlyCollection<ProblemTag> Evaluate(TeamStats team, TeamStats opponent, RulesProfile profile);
    }
}
