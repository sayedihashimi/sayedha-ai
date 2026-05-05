namespace DotnetSkills.Models;

public record PluginInfo
{
    public required string Name { get; init; }
    public string? Version { get; init; }
    public required string Description { get; init; }
    public List<string> SkillPaths { get; init; } = [];
    public string? SourcePath { get; init; }
}
