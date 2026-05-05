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
        var matchingKeywords = profile.GetRelevantKeywords();
        if (matchingKeywords.Count == 0)
            return [];

        return allSkills
            .Where(s => matchingKeywords.Any(kw =>
                s.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                (s.PluginName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false)))
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
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
            if (Directory.GetFiles(_repoRoot, "*.razor", SearchOption.AllDirectories).Length > 0)
                profile.HasBlazor = true;

            if (Directory.GetFiles(_repoRoot, "*.maui.*", SearchOption.AllDirectories).Length > 0)
                profile.HasMaui = true;

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

    public List<string> GetRelevantKeywords()
    {
        var keywords = new List<string>();

        // Always include general .NET keywords
        keywords.AddRange(["dotnet", "csharp"]);

        if (HasAspNet)
            keywords.AddRange(["aspnet", "asp.net", "web", "middleware", "endpoint", "minimal-api", "openapi"]);
        if (HasBlazor)
            keywords.AddRange(["blazor", "fluentui"]);
        if (HasMaui)
            keywords.AddRange(["maui"]);
        if (HasEfCore)
            keywords.AddRange(["ef-core", "entity-framework", "data"]);
        if (HasAI)
            keywords.AddRange(["ai", "ml", "semantic-kernel", "openai"]);
        if (HasTests)
            keywords.AddRange(["test", "mstest", "xunit", "nunit", "tunit"]);
        if (HasAspire)
            keywords.AddRange(["aspire"]);
        if (HasDocker)
            keywords.AddRange(["containerize", "docker"]);
        if (HasNuGetPackaging)
            keywords.AddRange(["nuget", "package"]);
        if (HasWorkerService)
            keywords.AddRange(["worker"]);

        return keywords;
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
