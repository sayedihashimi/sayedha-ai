using DotnetSkills.Catalog;
using DotnetSkills.GitHub;
using DotnetSkills.Models;

namespace DotnetSkills.Installation;

public class InstalledSkillTracker
{
    private readonly TargetDetector _detector;

    public InstalledSkillTracker(TargetDetector detector)
    {
        _detector = detector;
    }

    public List<InstalledSkill> GetInstalledSkills()
    {
        var installed = new List<InstalledSkill>();
        var targets = _detector.DetectTargets();

        if (targets.Count == 0)
        {
            // Check default location too
            var defaultTarget = _detector.GetDefaultTarget();
            if (Directory.Exists(defaultTarget.Path))
                targets.Add(defaultTarget);
        }

        foreach (var target in targets)
        {
            if (!Directory.Exists(target.Path))
                continue;

            foreach (var skillDir in Directory.GetDirectories(target.Path))
            {
                var skillMdPath = Path.Combine(skillDir, "SKILL.md");
                if (!File.Exists(skillMdPath))
                    continue;

                var content = File.ReadAllText(skillMdPath);
                var (name, description, license) = SkillMdParser.ParseFrontmatter(content);
                var dirName = Path.GetFileName(skillDir);

                installed.Add(new InstalledSkill
                {
                    Name = name ?? dirName,
                    Description = description ?? "",
                    License = license,
                    InstalledPath = skillDir,
                    TargetFolder = target,
                    ContentHash = GitHubContentFetcher.ComputeContentHash(content)
                });
            }
        }

        return installed;
    }

    public InstalledSkill? FindInstalled(string name)
    {
        return GetInstalledSkills()
            .FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsInstalled(string name)
    {
        return FindInstalled(name) != null;
    }
}

public record InstalledSkill
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? License { get; init; }
    public required string InstalledPath { get; init; }
    public required TargetFolder TargetFolder { get; init; }
    public required string ContentHash { get; init; }
}
