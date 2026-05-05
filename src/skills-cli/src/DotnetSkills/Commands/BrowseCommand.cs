using DotnetSkills.Catalog;
using DotnetSkills.Installation;
using Spectre.Console;

namespace DotnetSkills.Commands;

public class BrowseCommand
{
    private readonly CatalogCache _cache;
    private readonly InstalledSkillTracker _tracker;

    public BrowseCommand(CatalogCache cache, InstalledSkillTracker tracker)
    {
        _cache = cache;
        _tracker = tracker;
    }

    public async Task ExecuteAsync(bool refresh = false, CancellationToken ct = default)
    {
        var skills = await AnsiConsole.Status()
            .StartAsync("Fetching skill catalog...", async _ =>
                await _cache.GetSkillsAsync(refresh, ct));

        var installed = _tracker.GetInstalledSkills();
        var installedNames = installed.Select(i => i.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Available .NET Skills[/]")
            .AddColumn(new TableColumn("[bold]Name[/]").NoWrap())
            .AddColumn(new TableColumn("[bold]Source[/]"))
            .AddColumn(new TableColumn("[bold]Plugin[/]"))
            .AddColumn(new TableColumn("[bold]Status[/]"))
            .AddColumn(new TableColumn("[bold]Description[/]").Width(50));

        foreach (var skill in skills)
        {
            var status = installedNames.Contains(skill.Name)
                ? "[green]Installed[/]"
                : "[dim]Not installed[/]";

            table.AddRow(
                $"[cyan]{skill.Name.EscapeMarkup()}[/]",
                skill.Source.EscapeMarkup(),
                skill.PluginName?.EscapeMarkup() ?? "[dim]—[/]",
                status,
                TruncateDescription(skill.Description).EscapeMarkup());
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {skills.Count} skills[/]");
    }

    private static string TruncateDescription(string desc)
    {
        if (desc.Length <= 80) return desc;
        return desc[..77] + "...";
    }
}
