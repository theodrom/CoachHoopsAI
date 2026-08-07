namespace CoachHoopsAI.Admin.Helpers
{
    // User-facing text formatting only. Internal identifiers (rules-profile keys,
    // ProblemTag ordinals as received from the API) are never changed anywhere else
    // in Admin - they're only reformatted for display here, right before rendering.
    public static class DisplayFormatting
    {
        // "Amateur_Default" -> "Amateur (Default)", "Amateur_Development" -> "Amateur (Development)".
        // Purely cosmetic: splits the internal profile key on '_' and presents the
        // level/base name followed by its qualifier(s) in parentheses. Falls back to
        // the raw value for any shape that doesn't fit that pattern rather than
        // guessing, so an unrecognized profile name is still visible, not hidden.
        public static string HumanizeRulesProfileName(string? profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return "-";

            var parts = profileName.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length <= 1)
                return profileName;

            var baseName = parts[0];
            var qualifier = string.Join(" ", parts.Skip(1));
            return $"{baseName} ({qualifier})";
        }
    }
}
