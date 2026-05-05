using DotnetSkills.GitHub;
using DotnetSkills.Models;

namespace DotnetSkills.Installation;

public class SkillInstaller
{
    private readonly GitHubContentFetcher _fetcher;

    public SkillInstaller(GitHubContentFetcher fetcher)
    {
        _fetcher = fetcher;
    }

    public async Task InstallAsync(SkillInfo skill, TargetFolder target, CancellationToken ct = default)
    {
        var installDir = Path.Combine(target.Path, skill.Name);

        if (Directory.Exists(installDir))
        {
            Directory.Delete(installDir, recursive: true);
        }

        Directory.CreateDirectory(installDir);

        await _fetcher.DownloadSkillToAsync(
            skill.RepoOwner,
            skill.RepoName,
            skill.FolderPath,
            installDir,
            ct);
    }

    public async Task UpdateAsync(SkillInfo skill, string installedPath, CancellationToken ct = default)
    {
        if (Directory.Exists(installedPath))
        {
            Directory.Delete(installedPath, recursive: true);
        }

        Directory.CreateDirectory(installedPath);

        await _fetcher.DownloadSkillToAsync(
            skill.RepoOwner,
            skill.RepoName,
            skill.FolderPath,
            installedPath,
            ct);
    }

    public static void Uninstall(string installedPath)
    {
        if (Directory.Exists(installedPath))
        {
            Directory.Delete(installedPath, recursive: true);
        }
    }
}
