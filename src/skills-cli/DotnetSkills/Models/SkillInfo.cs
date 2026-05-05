namespace DotnetSkills.Models;

public record SkillInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? License { get; init; }
    public required string Source { get; init; } // e.g., "dotnet/skills", "github/awesome-copilot"
    public string? PluginName { get; init; } // parent plugin name if from dotnet/skills
    public required string RepoOwner { get; init; }
    public required string RepoName { get; init; }
    public required string FolderPath { get; init; } // path in source repo
    public List<string> Files { get; init; } = [];
    public string? ContentHash { get; init; }
}
