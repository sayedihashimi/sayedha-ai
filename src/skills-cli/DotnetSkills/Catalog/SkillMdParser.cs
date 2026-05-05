namespace DotnetSkills.Catalog;

public static class SkillMdParser
{
    /// <summary>
    /// Parses YAML frontmatter from a SKILL.md file.
    /// Returns (name, description, license).
    /// </summary>
    public static (string? Name, string? Description, string? License) ParseFrontmatter(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (null, null, null);

        var lines = content.Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---")
            return (null, null, null);

        string? name = null;
        string? description = null;
        string? license = null;

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line == "---")
                break;

            if (line.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
            {
                name = ExtractValue(line);
            }
            else if (line.StartsWith("description:", StringComparison.OrdinalIgnoreCase))
            {
                description = ExtractValue(line);
            }
            else if (line.StartsWith("license:", StringComparison.OrdinalIgnoreCase))
            {
                license = ExtractValue(line);
            }
        }

        return (name, description, license);
    }

    private static string? ExtractValue(string line)
    {
        var colonIdx = line.IndexOf(':');
        if (colonIdx < 0 || colonIdx >= line.Length - 1)
            return null;

        var value = line[(colonIdx + 1)..].Trim();
        // Remove surrounding quotes
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
