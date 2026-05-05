using System.Xml.Linq;
using DotnetSkills.Models;

namespace DotnetSkills.Catalog;

/// <summary>
/// Detects the type of .NET projects in a repo and recommends relevant skills.
/// </summary>
public class ProjectDetector
{
    private readonly string _repoRoot;

    public ProjectDetector(string repoRoot)
    {
        _repoRoot = repoRoot;
    }

    public ProjectProfile DetectProfile()
    {
        var profile = new ProjectProfile();

        // Scan for csproj files
        var csprojFiles = Directory.GetFiles(_repoRoot, "*.csproj", SearchOption.AllDirectories);
        foreach (var csproj in csprojFiles)
        {
            AnalyzeCsproj(csproj, profile);
        }

        // Scan for file patterns
        ScanFilePatterns(profile);

        return profile;
    }

    public List<SkillInfo> GetRecommendedSkills(List<SkillInfo> allSkills, ProjectProfile profile)
    {
        var rules = profile.GetRecommendationRules();
        if (rules.Count == 0)
            return [];

        // Score each skill: how many detected project types it matches
        var scored = new List<(SkillInfo Skill, int Score)>();
        foreach (var skill in allSkills)
        {
            int score = 0;
            foreach (var rule in rules)
            {
                if (rule.Matches(skill))
                    score += rule.Weight;
            }
            if (score > 0)
                scored.Add((skill, score));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Skill.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Skill)
            .ToList();
    }

    private static void AnalyzeCsproj(string path, ProjectProfile profile)
    {
        try
        {
            var doc = XDocument.Load(path);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            // Check SDK type
            var sdk = doc.Root?.Attribute("Sdk")?.Value ?? "";
            if (sdk.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
                profile.HasAspNet = true;
            if (sdk.Contains("Microsoft.NET.Sdk.Maui", StringComparison.OrdinalIgnoreCase))
                profile.HasMaui = true;
            if (sdk.Contains("Microsoft.NET.Sdk.Worker", StringComparison.OrdinalIgnoreCase))
                profile.HasWorkerService = true;

            // Check PackageReferences
            var packageRefs = doc.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e => e.Attribute("Include")?.Value ?? "")
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();

            foreach (var pkg in packageRefs)
            {
                if (pkg.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) ||
                    pkg.Contains("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
                    profile.HasEfCore = true;

                if (pkg.Contains("Microsoft.Extensions.AI", StringComparison.OrdinalIgnoreCase) ||
                    pkg.Contains("Microsoft.SemanticKernel", StringComparison.OrdinalIgnoreCase) ||
                    pkg.Contains("Microsoft.ML", StringComparison.OrdinalIgnoreCase) ||
                    pkg.Contains("Azure.AI.OpenAI", StringComparison.OrdinalIgnoreCase))
                    profile.HasAI = true;

                if (pkg.Contains("MSTest", StringComparison.OrdinalIgnoreCase) ||
                    pkg.Contains("xunit", StringComparison.OrdinalIgnoreCase) ||
                    pkg.Contains("NUnit", StringComparison.OrdinalIgnoreCase) ||
                    pkg.Contains("TUnit", StringComparison.OrdinalIgnoreCase))
                    profile.HasTests = true;

                if (pkg.Contains("Aspire", StringComparison.OrdinalIgnoreCase))
                    profile.HasAspire = true;

                if (pkg.Contains("Microsoft.Maui", StringComparison.OrdinalIgnoreCase))
                    profile.HasMaui = true;

                if (pkg.Contains("FluentUI.Blazor", StringComparison.OrdinalIgnoreCase))
                    profile.HasBlazor = true;

                if (pkg.Contains("Microsoft.AspNetCore.Components", StringComparison.OrdinalIgnoreCase))
                    profile.HasBlazor = true;
            }

            // Check for IsTestProject property
            var isTestProject = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "IsTestProject")?.Value;
            if (isTestProject?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                profile.HasTests = true;
        }
        catch
        {
            // Skip files that can't be parsed
        }
    }

    private void ScanFilePatterns(ProjectProfile profile)
    {
        try
        {
            // Only detect Blazor from .razor files if there are also Blazor-specific
            // component files (not just _ViewImports.razor or _Host.razor used by Razor Pages)
            var razorFiles = Directory.GetFiles(_repoRoot, "*.razor", SearchOption.AllDirectories);
            var blazorComponentFiles = razorFiles
                .Where(f =>
                {
                    var name = Path.GetFileName(f);
                    return !name.StartsWith('_') && !name.Equals("App.razor", StringComparison.OrdinalIgnoreCase);
                })
                .ToArray();
            if (blazorComponentFiles.Length > 3)
                profile.HasBlazor = true;

            // Check for Dockerfile or docker-compose (containerization)
            if (File.Exists(Path.Combine(_repoRoot, "Dockerfile")) ||
                File.Exists(Path.Combine(_repoRoot, "docker-compose.yml")) ||
                File.Exists(Path.Combine(_repoRoot, "docker-compose.yaml")))
                profile.HasDocker = true;

            // Check for NuGet packaging
            if (Directory.GetFiles(_repoRoot, "*.nuspec", SearchOption.AllDirectories).Length > 0)
                profile.HasNuGetPackaging = true;
        }
        catch
        {
            // Ignore scan errors
        }
    }
}

public class ProjectProfile
{
    public bool HasAspNet { get; set; }
    public bool HasBlazor { get; set; }
    public bool HasMaui { get; set; }
    public bool HasEfCore { get; set; }
    public bool HasAI { get; set; }
    public bool HasTests { get; set; }
    public bool HasAspire { get; set; }
    public bool HasWorkerService { get; set; }
    public bool HasDocker { get; set; }
    public bool HasNuGetPackaging { get; set; }

    public bool HasAnyDetection =>
        HasAspNet || HasBlazor || HasMaui || HasEfCore || HasAI ||
        HasTests || HasAspire || HasWorkerService || HasDocker || HasNuGetPackaging;

    public List<RecommendationRule> GetRecommendationRules()
    {
        var rules = new List<RecommendationRule>();

        if (HasAspNet)
        {
            rules.Add(new RecommendationRule(
                Weight: 3,
                PluginNames: ["dotnet-aspnet"],
                SkillNamePatterns: ["aspnet", "minimal-api", "openapi", "containerize-aspnet"],
                DescriptionPatterns: []));
        }

        if (HasBlazor)
        {
            rules.Add(new RecommendationRule(
                Weight: 3,
                PluginNames: [],
                SkillNamePatterns: ["blazor", "fluentui-blazor"],
                DescriptionPatterns: []));
        }

        if (HasMaui)
        {
            rules.Add(new RecommendationRule(
                Weight: 3,
                PluginNames: ["dotnet-maui"],
                SkillNamePatterns: ["maui"],
                DescriptionPatterns: []));
        }

        if (HasEfCore)
        {
            rules.Add(new RecommendationRule(
                Weight: 3,
                PluginNames: ["dotnet-data"],
                SkillNamePatterns: ["ef-core", "entity-framework", "cosmosdb"],
                DescriptionPatterns: []));
        }

        if (HasAI)
        {
            rules.Add(new RecommendationRule(
                Weight: 3,
                PluginNames: ["dotnet-ai"],
                SkillNamePatterns: ["semantic-kernel", "openai"],
                DescriptionPatterns: []));
        }

        if (HasTests)
        {
            rules.Add(new RecommendationRule(
                Weight: 2,
                PluginNames: ["dotnet-test"],
                SkillNamePatterns: ["mstest", "xunit", "nunit", "tunit"],
                DescriptionPatterns: []));
        }

        if (HasAspire)
        {
            rules.Add(new RecommendationRule(
                Weight: 3,
                PluginNames: [],
                SkillNamePatterns: ["aspire"],
                DescriptionPatterns: []));
        }

        if (HasDocker)
        {
            rules.Add(new RecommendationRule(
                Weight: 2,
                PluginNames: [],
                SkillNamePatterns: ["containerize-aspnet", "containerize-aspnetcore"],
                DescriptionPatterns: []));
        }

        if (HasNuGetPackaging)
        {
            rules.Add(new RecommendationRule(
                Weight: 2,
                PluginNames: ["dotnet-nuget"],
                SkillNamePatterns: ["nuget"],
                DescriptionPatterns: []));
        }

        return rules;
    }

    public string GetSummary()
    {
        var parts = new List<string>();
        if (HasAspNet) parts.Add("ASP.NET Core");
        if (HasBlazor) parts.Add("Blazor");
        if (HasMaui) parts.Add("MAUI");
        if (HasEfCore) parts.Add("EF Core");
        if (HasAI) parts.Add("AI/ML");
        if (HasTests) parts.Add("Testing");
        if (HasAspire) parts.Add("Aspire");
        if (HasDocker) parts.Add("Docker");
        if (HasNuGetPackaging) parts.Add("NuGet packaging");
        if (HasWorkerService) parts.Add("Worker Service");
        return parts.Count > 0 ? string.Join(", ", parts) : "General .NET";
    }
}

public record RecommendationRule(
    int Weight,
    string[] PluginNames,
    string[] SkillNamePatterns,
    string[] DescriptionPatterns)
{
    public bool Matches(SkillInfo skill)
    {
        // Match by plugin name (exact, case-insensitive)
        if (skill.PluginName != null &&
            PluginNames.Any(p => skill.PluginName.Equals(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Match by skill name patterns (substring)
        if (SkillNamePatterns.Any(p => skill.Name.Contains(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Match by description patterns (substring) — only if explicitly provided
        if (DescriptionPatterns.Length > 0 &&
            DescriptionPatterns.Any(p => skill.Description.Contains(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }
}
