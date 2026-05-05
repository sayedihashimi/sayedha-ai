using DotnetSkills.Installation;
using Spectre.Console;

namespace DotnetSkills.Commands;

public class UninstallCommand
{
    private readonly InstalledSkillTracker _tracker;

    public UninstallCommand(InstalledSkillTracker tracker)
    {
        _tracker = tracker;
    }

    public Task ExecuteAsync(string name, CancellationToken ct = default)
    {
        var installed = _tracker.FindInstalled(name);
        if (installed == null)
        {
            AnsiConsole.MarkupLine($"[red]Skill '[bold]{name.EscapeMarkup()}[/]' is not installed.[/]");
            return Task.CompletedTask;
        }

        AnsiConsole.MarkupLine($"[bold]{installed.Name}[/] at [dim]{installed.InstalledPath}[/]");
        var confirm = AnsiConsole.Confirm($"Remove this skill?", defaultValue: false);
        if (!confirm) return Task.CompletedTask;

        SkillInstaller.Uninstall(installed.InstalledPath);
        AnsiConsole.MarkupLine($"[green]✓[/] Uninstalled [bold]{installed.Name.EscapeMarkup()}[/]");
        return Task.CompletedTask;
    }
}
