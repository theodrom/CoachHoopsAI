

namespace CoachHoopsAI.Domain.Rules
{
    public class ResolvedRulesProfile
    {
        public string Name { get; }
        public RulesProfile Profile { get; }

        public ResolvedRulesProfile(string name, RulesProfile profile)
        {
            Name = name;
            Profile = profile;
        }
    }
}
