using DotnetSkills.Models;

namespace DotnetSkills.Catalog;

public class CatalogManager
{
    private readonly List<ICatalogSource> _sources;
    private List<SkillInfo>? _cachedSkills;

    public CatalogManager(IEnumerable<ICatalogSource> sources)
    {
        _sources = sources.ToList();
    }

    public async Task<List<SkillInfo>> GetAllSkillsAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (_cachedSkills != null && !forceRefresh)
            return _cachedSkills;

        var allSkills = new List<SkillInfo>();
        foreach (var source in _sources)
        {
            var skills = await source.GetSkillsAsync(ct);
            allSkills.AddRange(skills);
        }

        // Deduplicate by name, preferring dotnet/skills source
        _cachedSkills = allSkills
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(s => s.Source == "dotnet/skills" ? 0 : 1).First())
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return _cachedSkills;
    }

    public async Task<List<SkillInfo>> SearchAsync(string query, CancellationToken ct = default)
    {
        var all = await GetAllSkillsAsync(ct: ct);
        if (string.IsNullOrWhiteSpace(query))
            return all;

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return all.Where(s => terms.All(t =>
            s.Name.Contains(t, StringComparison.OrdinalIgnoreCase) ||
            s.Description.Contains(t, StringComparison.OrdinalIgnoreCase) ||
            (s.PluginName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false)
        )).ToList();
    }

    public async Task<SkillInfo?> FindByNameAsync(string name, CancellationToken ct = default)
    {
        var all = await GetAllSkillsAsync(ct: ct);
        return all.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
