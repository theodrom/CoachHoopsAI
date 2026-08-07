using CoachHoopsAI.Domain.Enums;
using CoachHoopsAI.Domain.Rules;

namespace CoachHoopsAI.Application.Tests.TestDoubles;

public class FakeRulesProfileProvider : IRulesProfileProvider
{
    public ResolvedRulesProfile ProfileToReturn { get; set; } = new("Fake_Profile", new RulesProfile());

    public Level? LastLevel { get; private set; }
    public string? LastOverrideProfileName { get; private set; }
    public int CallCount { get; private set; }

    public ResolvedRulesProfile Resolve(Level level, string? overrideProfileName)
    {
        CallCount++;
        LastLevel = level;
        LastOverrideProfileName = overrideProfileName;
        return ProfileToReturn;
    }
}
