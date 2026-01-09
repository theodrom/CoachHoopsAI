using CoachHoopsAI.Domain.Enums;
using Microsoft.Extensions.Options;

namespace CoachHoopsAI.Domain.Rules
{
    public class RulesProfileProvider : IRulesProfileProvider
    {
        private readonly RulesProfilesOptions _options;

        public RulesProfileProvider(IOptions<RulesProfilesOptions> options)
        {
            _options = options.Value ?? new RulesProfilesOptions();
        }

        public ResolvedRulesProfile Resolve(Level level, string? overrideProfileName)
        {
            // 1) explicit override wins if it exists
            if (!string.IsNullOrWhiteSpace(overrideProfileName))
            {
                var key = overrideProfileName.Trim();
                if (_options.Profiles.TryGetValue(key, out var profile))
                    return new ResolvedRulesProfile(key, profile);
            }

            // 2) default by level
            var levelKey = level.ToString(); // EasyBasket, Youth, Amateur, Pro
            if (_options.DefaultProfileByLevel.TryGetValue(levelKey, out var defaultName)
                && _options.Profiles.TryGetValue(defaultName, out var defaultProfile))
            {
                return new ResolvedRulesProfile(defaultName, defaultProfile);
            }

            // 3) final fallback
            return new ResolvedRulesProfile("Fallback_Default", new RulesProfile());
        }
    }
}
