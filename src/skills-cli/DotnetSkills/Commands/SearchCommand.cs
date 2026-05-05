using DotnetSkills.Catalog;
using DotnetSkills.Installation;
using Spectre.Console;

namespace DotnetSkills.Commands;

public class SearchCommand
{
    private readonly CatalogCache _cache;
    private readonly InstalledSkillTracker _tracker;

    public SearchCommand(CatalogCache cache, InstalledSkillTracker tracker)
    {
        _cache = cache;
        _tracker = tracker;
    }

    public async Task ExecuteAsync(string query, CancellationToken ct = default)
    {
        var results = await AnsiConsole.Status()
            .StartAsync($"Searching for '{query}'...", async _ =>
                await _cache.SearchAsync(query, ct: ct));

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No skills found matching '[bold]{query.EscapeMarkup()}[/]'[/]");
            return;
        }

        var installed = _tracker.GetInstalledSkills();
        var installedNames = installed.Select(i => i.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold blue]Search Results for '{query.EscapeMarkup()}'[/]")
            .AddColumn(new TableColumn("[bold]Name[/]").NoWrap())
            .AddColumn(new TableColumn("[bold]Source[/]"))
            .AddColumn(new TableColumn("[bold]Status[/]"))
            .AddColumn(new TableColumn("[bold]Description[/]").Width(50));

        foreach (var skill in results)
        {
            var status = installedNames.Contains(skill.Name)
                ? "[green]Installed[/]"
                : "[dim]Not installed[/]";

            table.AddRow(
                $"[cyan]{skill.Name.EscapeMarkup()}[/]",
                skill.Source.EscapeMarkup(),
                status,
                TruncateDescription(skill.Description).EscapeMarkup());
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]{results.Count} result(s)[/]");
    }

    private static string TruncateDescription(string desc)
    {
        if (desc.Length <= 80) return desc;
        return desc[..77] + "...";
    }
}
