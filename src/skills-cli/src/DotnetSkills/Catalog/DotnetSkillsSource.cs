using System.Text.Json.Serialization;
using DotnetSkills.GitHub;
using DotnetSkills.Models;

namespace DotnetSkills.Catalog;

public class DotnetSkillsSource : ICatalogSource
{
    private const string Owner = "dotnet";
    private const string Repo = "skills";
    private const string MarketplacePath = ".github/plugin/marketplace.json";

    private readonly GitHubApiClient _client;

    public string SourceName => "dotnet/skills";

    public DotnetSkillsSource(GitHubApiClient client)
    {
        _client = client;
    }

    public async Task<List<SkillInfo>> GetSkillsAsync(CancellationToken ct = default)
    {
        var skills = new List<SkillInfo>();

        var marketplace = await _client.GetJsonFileAsync<MarketplaceJson>(Owner, Repo, MarketplacePath, ct);
        if (marketplace?.Plugins == null)
            return skills;

        foreach (var pluginEntry in marketplace.Plugins)
        {
            var pluginPath = pluginEntry.Source.TrimStart('.', '/');
            var pluginJsonPath = $"{pluginPath}/plugin.json";

            PluginJsonModel? pluginJson;
            try
            {
                pluginJson = await _client.GetJsonFileAsync<PluginJsonModel>(Owner, Repo, pluginJsonPath, ct);
            }
            catch
            {
                continue;
            }

            if (pluginJson == null) continue;

            // List skill directories under the plugin's skills path
            var skillsBasePath = $"{pluginPath}/skills";
            List<GitHubContent> skillDirs;
            try
            {
                skillDirs = await _client.GetDirectoryContentsAsync(Owner, Repo, skillsBasePath, ct);
            }
            catch
            {
                continue;
            }

            foreach (var skillDir in skillDirs.Where(d => d.Type == "dir"))
            {
                var skillMdPath = $"{skillDir.Path}/SKILL.md";
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
                    Name = name ?? skillDir.Name,
                    Description = description ?? pluginEntry.Description,
                    License = license,
                    Source = SourceName,
                    PluginName = pluginEntry.Name,
                    RepoOwner = Owner,
                    RepoName = Repo,
                    FolderPath = skillDir.Path,
                    ContentHash = GitHubContentFetcher.ComputeContentHash(skillMdContent)
                });
            }
        }

        return skills;
    }

    private record MarketplaceJson
    {
        public string Name { get; init; } = "";
        public List<MarketplacePlugin> Plugins { get; init; } = [];
    }

    private record MarketplacePlugin
    {
        public string Name { get; init; } = "";
        public string Source { get; init; } = "";
        public string Description { get; init; } = "";
    }

    private record PluginJsonModel
    {
        public string Name { get; init; } = "";
        public string Version { get; init; } = "";
        public string Description { get; init; } = "";
        public List<string> Skills { get; init; } = [];
        [JsonPropertyName("lspServers")]
        public string? LspServers { get; init; }
    }
}
