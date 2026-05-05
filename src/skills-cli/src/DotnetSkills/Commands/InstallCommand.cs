using DotnetSkills.Catalog;
using DotnetSkills.Installation;
using DotnetSkills.Models;
using Spectre.Console;

namespace DotnetSkills.Commands;

public class InstallCommand
{
    private readonly CatalogCache _cache;
    private readonly SkillInstaller _installer;
    private readonly TargetDetector _detector;
    private readonly InstalledSkillTracker _tracker;

    public InstallCommand(CatalogCache cache, SkillInstaller installer, TargetDetector detector, InstalledSkillTracker tracker)
    {
        _cache = cache;
        _installer = installer;
        _detector = detector;
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

        if (_tracker.IsInstalled(name))
        {
            var overwrite = AnsiConsole.Confirm($"[yellow]'{name}' is already installed. Overwrite?[/]", defaultValue: false);
            if (!overwrite) return;
        }

        var target = SelectTarget();
        if (target == null) return;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Installing {skill.Name}...", async _ =>
            {
                await _installer.InstallAsync(skill, target, ct);
            });

        AnsiConsole.MarkupLine($"[green]✓[/] Installed [bold]{skill.Name.EscapeMarkup()}[/] to [dim]{target.RelativePath}/{skill.Name}[/]");
    }

    private TargetFolder? SelectTarget()
    {
        var targets = _detector.DetectTargets();

        if (targets.Count == 0)
        {
            var defaultTarget = _detector.GetDefaultTarget();
            AnsiConsole.MarkupLine($"[dim]No AI client folders found. Using default: {defaultTarget.RelativePath}[/]");
            Directory.CreateDirectory(defaultTarget.Path);
            return defaultTarget;
        }

        if (targets.Count == 1)
            return targets[0];

        // Multiple targets found — prompt user
        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<TargetFolder>()
                .Title("Multiple AI client folders detected. Select where to install:")
                .PageSize(10)
                .AddChoices(targets)
                .UseConverter(t => $"{t.ClientName} ({t.RelativePath})"));

        return selected.FirstOrDefault();
    }
}
