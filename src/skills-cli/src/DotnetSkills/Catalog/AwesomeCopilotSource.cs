using DotnetSkills.GitHub;
using DotnetSkills.Models;

namespace DotnetSkills.Catalog;

public class AwesomeCopilotSource : ICatalogSource
{
    private const string Owner = "github";
    private const string Repo = "awesome-copilot";
    private const string SkillsPath = "skills";

    private readonly GitHubApiClient _client;

    public string SourceName => "github/awesome-copilot";

    // Keywords used to identify .NET-related skills
    private static readonly string[] DotNetKeywords =
    [
        "dotnet", "csharp", "c#", ".net", "aspnet", "asp.net",
        "blazor", "maui", "ef-core", "entity-framework", "nuget",
        "msbuild", "aspire", "fluentui-blazor", "containerize-aspnet",
        "containerize-aspnetcore", "mstest", "nunit", "xunit", "tunit",
        "azure-static-web-apps"
    ];

    public AwesomeCopilotSource(GitHubApiClient client)
    {
        _client = client;
    }

    public async Task<List<SkillInfo>> GetSkillsAsync(CancellationToken ct = default)
    {
        var skills = new List<SkillInfo>();

        List<GitHubContent> skillDirs;
        try
        {
            skillDirs = await _client.GetDirectoryContentsAsync(Owner, Repo, SkillsPath, ct);
        }
        catch
        {
            return skills;
        }

        var directories = skillDirs.Where(d => d.Type == "dir").ToList();

        // First pass: filter by name
        var potentialDotNet = directories.Where(d => IsDotNetRelatedByName(d.Name)).ToList();

        // For each potential match, fetch SKILL.md and verify
        foreach (var dir in potentialDotNet)
        {
            var skillMdPath = $"{dir.Path}/SKILL.md";
            string skillMdContent;
            try
            {
                skillMdContent = await _client.GetFileContentAsync(Owner, Repo, skillMdPath, ct);
            }
            catch
            {
                continue;
            }

            var (name, description, license) = SkillMdParser.ParseFrontmatter(skillMdContent);

            skills.Add(new SkillInfo
            {
                Name = name ?? dir.Name,
                Description = description ?? "",
                License = license,
                Source = SourceName,
                PluginName = null,
                RepoOwner = Owner,
                RepoName = Repo,
                FolderPath = dir.Path,
                ContentHash = GitHubContentFetcher.ComputeContentHash(skillMdContent)
            });
        }

        return skills;
    }

    private static bool IsDotNetRelatedByName(string name)
    {
        var lower = name.ToLowerInvariant();
        return DotNetKeywords.Any(kw => lower.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }
}
