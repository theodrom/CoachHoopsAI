using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Rules;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Application.Tests.TestDoubles;

// Hand-written test double (no mocking framework) that records the arguments it
// was called with so orchestration tests can assert on them, and returns a
// pre-configured result instead of running real threshold logic.
public class FakeStatRulesEngine : IStatRulesEngine
{
    public IReadOnlyCollection<ProblemTag> TagsToReturn { get; set; } = Array.Empty<ProblemTag>();

    public TeamStats? LastTeam { get; private set; }
    public TeamStats? LastOpponent { get; private set; }
    public RulesProfile? LastProfile { get; private set; }
    public int CallCount { get; private set; }

    public IReadOnlyCollection<ProblemTag> Evaluate(TeamStats team, TeamStats opponent, RulesProfile profile)
    {
        CallCount++;
        LastTeam = team;
        LastOpponent = opponent;
        LastProfile = profile;
        return TagsToReturn;
    }
}
