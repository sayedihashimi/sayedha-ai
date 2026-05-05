using DotnetSkills.Catalog;
using DotnetSkills.Installation;
using Spectre.Console;

namespace DotnetSkills.Commands;

public class InfoCommand
{
    private readonly CatalogCache _cache;
    private readonly InstalledSkillTracker _tracker;

    public InfoCommand(CatalogCache cache, InstalledSkillTracker tracker)
    {
        _cache = cache;
        _tracker = tracker;
    }

    public async Task ExecuteAsync(string name, CancellationToken ct = default)
    {
        var skill = await _cache.FindByNameAsync(name, ct: ct);
        if (skill == null)
        {
            AnsiConsole.MarkupLine($"[red]Skill '[bold]{name.EscapeMarkup()}[/]' not found in catalog.[/]");
            return;
        }

        var installed = _tracker.FindInstalled(name);

        var panel = new Panel(new Rows(
            new Markup($"[bold]Name:[/]        {skill.Name.EscapeMarkup()}"),
            new Markup($"[bold]Description:[/] {skill.Description.EscapeMarkup()}"),
            new Markup($"[bold]Source:[/]      {skill.Source.EscapeMarkup()}"),
            new Markup($"[bold]Plugin:[/]      {(skill.PluginName ?? "—").EscapeMarkup()}"),
            new Markup($"[bold]License:[/]     {(skill.License ?? "—").EscapeMarkup()}"),
            new Markup($"[bold]Repo Path:[/]   {skill.FolderPath.EscapeMarkup()}"),
            new Markup($"[bold]Installed:[/]   {(installed != null ? $"[green]Yes[/] at {installed.InstalledPath.EscapeMarkup()}" : "[dim]No[/]")}")
        ))
        {
            Header = new PanelHeader($"[bold blue] {skill.Name.EscapeMarkup()} [/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
    }
}
