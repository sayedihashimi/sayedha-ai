using System.Security.Cryptography;
using System.Text;

namespace DotnetSkills.GitHub;

public class GitHubContentFetcher
{
    private readonly GitHubApiClient _client;

    public GitHubContentFetcher(GitHubApiClient client)
    {
        _client = client;
    }

    public async Task<string> FetchSkillMdAsync(string owner, string repo, string skillFolderPath, CancellationToken ct = default)
    {
        var skillMdPath = $"{skillFolderPath.TrimEnd('/')}/SKILL.md";
        return await _client.GetFileContentAsync(owner, repo, skillMdPath, ct);
    }

    public async Task<List<string>> ListSkillFilesAsync(string owner, string repo, string skillFolderPath, CancellationToken ct = default)
    {
        var files = new List<string>();
        await CollectFilesRecursiveAsync(owner, repo, skillFolderPath.TrimEnd('/'), files, ct);
        return files;
    }

    private async Task CollectFilesRecursiveAsync(string owner, string repo, string path, List<string> files, CancellationToken ct)
    {
        var contents = await _client.GetDirectoryContentsAsync(owner, repo, path, ct);
        foreach (var item in contents)
        {
            if (item.Type == "file")
            {
                files.Add(item.Path);
            }
            else if (item.Type == "dir")
            {
                await CollectFilesRecursiveAsync(owner, repo, item.Path, files, ct);
            }
        }
    }

    public async Task DownloadSkillToAsync(string owner, string repo, string skillFolderPath, string targetDir, CancellationToken ct = default)
    {
        var files = await ListSkillFilesAsync(owner, repo, skillFolderPath, ct);
        var basePath = skillFolderPath.TrimEnd('/');

        foreach (var filePath in files)
        {
            var relativePath = filePath.StartsWith(basePath)
                ? filePath[(basePath.Length + 1)..]
                : filePath;

            var localPath = Path.Combine(targetDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(localPath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            var content = await _client.DownloadFileAsync(owner, repo, filePath, ct);
            await File.WriteAllBytesAsync(localPath, content, ct);
        }
    }

    public static string ComputeContentHash(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(hash);
    }
}
