namespace DotnetSkills.Models;

public record CatalogEntry
{
    public required SkillInfo Skill { get; init; }
    public bool IsInstalled { get; init; }
    public bool HasUpdate { get; init; }
    public string? InstalledPath { get; init; }
}
