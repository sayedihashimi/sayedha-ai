using System.Text.Json;
using DotnetSkills.Models;

namespace DotnetSkills.Catalog;

public class CatalogCache
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".dotnet-skills", "cache");

    private static readonly string CacheFile = Path.Combine(CacheDir, "catalog.json");
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly CatalogManager _manager;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public CatalogCache(CatalogManager manager)
    {
        _manager = manager;
    }

    public async Task<List<SkillInfo>> GetSkillsAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh)
        {
            var cached = await TryLoadCacheAsync(ct);
            if (cached != null)
                return cached;
        }

        var skills = await _manager.GetAllSkillsAsync(forceRefresh: true, ct: ct);
        await SaveCacheAsync(skills, ct);
        return skills;
    }

    public async Task<List<SkillInfo>> SearchAsync(string query, bool forceRefresh = false, CancellationToken ct = default)
    {
        var all = await GetSkillsAsync(forceRefresh, ct);
        if (string.IsNullOrWhiteSpace(query))
            return all;

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return all.Where(s => terms.All(t =>
            s.Name.Contains(t, StringComparison.OrdinalIgnoreCase) ||
            s.Description.Contains(t, StringComparison.OrdinalIgnoreCase) ||
            (s.PluginName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false)
        )).ToList();
    }

    public async Task<SkillInfo?> FindByNameAsync(string name, bool forceRefresh = false, CancellationToken ct = default)
    {
        var all = await GetSkillsAsync(forceRefresh, ct);
        return all.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<SkillInfo>?> TryLoadCacheAsync(CancellationToken ct)
    {
        if (!File.Exists(CacheFile))
            return null;

        var fileInfo = new FileInfo(CacheFile);
        if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc > CacheTtl)
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(CacheFile, ct);
            return JsonSerializer.Deserialize<List<SkillInfo>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveCacheAsync(List<SkillInfo> skills, CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            var json = JsonSerializer.Serialize(skills, JsonOptions);
            await File.WriteAllTextAsync(CacheFile, json, ct);
        }
        catch
        {
            // Silently ignore cache write failures
        }
    }
}
