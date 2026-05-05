using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetSkills.GitHub;

public class GitHubApiClient : IDisposable
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GitHubApiClient()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-skills-cli/0.1");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
    }

    public async Task<List<GitHubContent>> GetDirectoryContentsAsync(string owner, string repo, string path, CancellationToken ct = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path.TrimStart('/')}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GitHubContent>>(JsonOptions, ct) ?? [];
    }

    public async Task<string> GetFileContentAsync(string owner, string repo, string path, CancellationToken ct = default)
    {
        var url = $"https://raw.githubusercontent.com/{owner}/{repo}/main/{path.TrimStart('/')}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<T?> GetJsonFileAsync<T>(string owner, string repo, string path, CancellationToken ct = default)
    {
        var content = await GetFileContentAsync(owner, repo, path, ct);
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    public async Task<List<GitHubTreeItem>> GetTreeRecursiveAsync(string owner, string repo, string path, CancellationToken ct = default)
    {
        // Use the Trees API for recursive listing
        // First get the ref to find the tree SHA
        var contents = await GetDirectoryContentsAsync(owner, repo, path, ct);
        return contents.Select(c => new GitHubTreeItem
        {
            Path = c.Path,
            Type = c.Type,
            Size = c.Size
        }).ToList();
    }

    public async Task<byte[]> DownloadFileAsync(string owner, string repo, string path, CancellationToken ct = default)
    {
        var url = $"https://raw.githubusercontent.com/{owner}/{repo}/main/{path.TrimStart('/')}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public void Dispose() => _http.Dispose();
}

public record GitHubContent
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string Type { get; init; } = ""; // "file" or "dir"
    public long Size { get; init; }
    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; init; }
}

public record GitHubTreeItem
{
    public string Path { get; init; } = "";
    public string Type { get; init; } = "";
    public long Size { get; init; }
}
