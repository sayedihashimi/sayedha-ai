using DotnetSkills.Installation;
using Spectre.Console;

namespace DotnetSkills.Commands;

public class ListCommand
{
    private readonly InstalledSkillTracker _tracker;

    public ListCommand(InstalledSkillTracker tracker)
    {
        _tracker = tracker;
    }

    public Task ExecuteAsync(CancellationToken ct = default)
    {
        var installed = _tracker.GetInstalledSkills();

        if (installed.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No skills are currently installed.[/]");
            return Task.CompletedTask;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Installed Skills[/]")
            .AddColumn(new TableColumn("[bold]Name[/]").NoWrap())
            .AddColumn(new TableColumn("[bold]Client[/]"))
            .AddColumn(new TableColumn("[bold]Path[/]"))
            .AddColumn(new TableColumn("[bold]Description[/]").Width(50));

        foreach (var skill in installed)
        {
            table.AddRow(
                $"[cyan]{skill.Name.EscapeMarkup()}[/]",
                skill.TargetFolder.ClientName.EscapeMarkup(),
                skill.InstalledPath.EscapeMarkup(),
                TruncateDescription(skill.Description).EscapeMarkup());
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {installed.Count} skill(s) installed[/]");
        return Task.CompletedTask;
    }

    private static string TruncateDescription(string desc)
    {
        if (desc.Length <= 80) return desc;
        return desc[..77] + "...";
    }
}
