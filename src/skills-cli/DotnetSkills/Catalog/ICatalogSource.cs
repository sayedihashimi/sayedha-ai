using DotnetSkills.Models;

namespace DotnetSkills.Catalog;

public interface ICatalogSource
{
    string SourceName { get; }
    Task<List<SkillInfo>> GetSkillsAsync(CancellationToken ct = default);
}
