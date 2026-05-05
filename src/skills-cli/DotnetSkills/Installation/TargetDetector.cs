namespace DotnetSkills.Installation;

public class TargetDetector
{
    // Well-known directories for different AI clients
    private static readonly (string Path, string ClientName)[] KnownTargets =
    [
        (".agents/skills", "GitHub Copilot"),
        (".claude-plugin", "Claude Code"),
        (".cursor-plugin", "Cursor"),
    ];

    private readonly string _repoRoot;

    public TargetDetector(string repoRoot)
    {
        _repoRoot = repoRoot;
    }

    public List<TargetFolder> DetectTargets()
    {
        var targets = new List<TargetFolder>();

        foreach (var (path, clientName) in KnownTargets)
        {
            var fullPath = Path.Combine(_repoRoot, path);
            if (Directory.Exists(fullPath))
            {
                targets.Add(new TargetFolder
                {
                    Path = fullPath,
                    RelativePath = path,
                    ClientName = clientName
                });
            }
        }

        return targets;
    }

    public TargetFolder GetDefaultTarget()
    {
        return new TargetFolder
        {
            Path = Path.Combine(_repoRoot, ".agents/skills"),
            RelativePath = ".agents/skills",
            ClientName = "GitHub Copilot"
        };
    }

    public static string FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return startDir;
    }
}

public record TargetFolder
{
    public required string Path { get; init; }
    public required string RelativePath { get; init; }
    public required string ClientName { get; init; }
}
