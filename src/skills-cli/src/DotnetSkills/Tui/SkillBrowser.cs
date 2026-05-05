using DotnetSkills.Catalog;
using DotnetSkills.Installation;
using DotnetSkills.Models;
using Spectre.Console;

namespace DotnetSkills.Tui;

public class SkillBrowser
{
    private readonly CatalogCache _cache;
    private readonly InstalledSkillTracker _tracker;
    private readonly SkillInstaller _installer;
    private readonly TargetDetector _detector;

    public SkillBrowser(CatalogCache cache, InstalledSkillTracker tracker, SkillInstaller installer, TargetDetector detector)
    {
        _cache = cache;
        _tracker = tracker;
        _installer = installer;
        _detector = detector;
    }

    public async Task BrowseAsync(CancellationToken ct = default)
    {
        var skills = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Loading skill catalog...", async _ =>
                await _cache.GetSkillsAsync(ct: ct));

        if (skills.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No skills available.[/]");
            return;
        }

        var installed = _tracker.GetInstalledSkills();
        var installedNames = installed.Select(i => i.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<SkillInfo>()
                .Title("[bold]Browse skills[/] [dim](use arrow keys, enter to view details)[/]")
                .PageSize(15)
                .HighlightStyle(new Style(Color.Cyan1))
                .AddChoices(skills)
                .UseConverter(s =>
                {
                    var status = installedNames.Contains(s.Name) ? " [green]✓[/]" : "";
                    var plugin = s.PluginName != null ? $" [dim]({s.PluginName})[/]" : "";
                    return $"[cyan]{s.Name.EscapeMarkup()}[/]{status}{plugin} - {TruncateDescription(s.Description, 60).EscapeMarkup()}";
                }));

        // Show detail and offer install
        ShowSkillDetail(selected, installedNames.Contains(selected.Name));

        if (!installedNames.Contains(selected.Name))
        {
            var install = AnsiConsole.Confirm("Install this skill?", defaultValue: false);
            if (install)
            {
                await InstallSkillAsync(selected, ct);
            }
        }
    }

    public async Task InstallFlowAsync(CancellationToken ct = default)
    {
        var skills = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Loading skill catalog...", async _ =>
                await _cache.GetSkillsAsync(ct: ct));

        if (skills.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No skills available.[/]");
            return;
        }

        var installed = _tracker.GetInstalledSkills();
        var installedNames = installed.Select(i => i.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var available = skills.Where(s => !installedNames.Contains(s.Name)).ToList();

        if (available.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All available skills are already installed.[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<SkillInfo>()
                .Title("[bold]Select skills to install[/] [dim](space to select, enter to confirm)[/]")
                .PageSize(15)
                .HighlightStyle(new Style(Color.Cyan1))
                .AddChoices(available)
                .UseConverter(s =>
                {
                    var plugin = s.PluginName != null ? $" ({s.PluginName})" : "";
                    return $"{s.Name}{plugin} - {TruncateDescription(s.Description, 50)}";
                }));

        if (selected.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No skills selected.[/]");
            return;
        }

        // Select target folder(s)
        var targets = _detector.DetectTargets();
        List<TargetFolder> selectedTargets;

        if (targets.Count == 0)
        {
            var defaultTarget = _detector.GetDefaultTarget();
            AnsiConsole.MarkupLine($"[dim]No AI client folders found. Will install to: {defaultTarget.RelativePath}[/]");
            Directory.CreateDirectory(defaultTarget.Path);
            selectedTargets = [defaultTarget];
        }
        else if (targets.Count == 1)
        {
            selectedTargets = targets;
        }
        else
        {
            selectedTargets = AnsiConsole.Prompt(
                new MultiSelectionPrompt<TargetFolder>()
                    .Title("Select target folder(s):")
                    .AddChoices(targets)
                    .UseConverter(t => $"{t.ClientName} ({t.RelativePath})"));
        }

        // Confirm
        AnsiConsole.MarkupLine($"\n[bold]Installing {selected.Count} skill(s) to {selectedTargets.Count} target(s):[/]");
        foreach (var s in selected)
            AnsiConsole.MarkupLine($"  • {s.Name}");
        AnsiConsole.MarkupLine($"[dim]Targets: {string.Join(", ", selectedTargets.Select(t => t.RelativePath))}[/]");

        if (!AnsiConsole.Confirm("\nProceed?"))
            return;

        // Install
        foreach (var target in selectedTargets)
        {
            foreach (var skill in selected)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync($"Installing {skill.Name} to {target.RelativePath}...", async _ =>
                    {
                        await _installer.InstallAsync(skill, target, ct);
                    });

                AnsiConsole.MarkupLine($"[green]✓[/] {skill.Name.EscapeMarkup()} → {target.RelativePath}");
            }
        }

        AnsiConsole.MarkupLine($"\n[green bold]Done![/] Installed {selected.Count} skill(s).");
    }

    private async Task InstallSkillAsync(SkillInfo skill, CancellationToken ct)
    {
        var targets = _detector.DetectTargets();
        TargetFolder target;

        if (targets.Count == 0)
        {
            target = _detector.GetDefaultTarget();
            Directory.CreateDirectory(target.Path);
        }
        else if (targets.Count == 1)
        {
            target = targets[0];
        }
        else
        {
            target = AnsiConsole.Prompt(
                new SelectionPrompt<TargetFolder>()
                    .Title("Select target folder:")
                    .AddChoices(targets)
                    .UseConverter(t => $"{t.ClientName} ({t.RelativePath})"));
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Installing {skill.Name}...", async _ =>
            {
                await _installer.InstallAsync(skill, target, ct);
            });

        AnsiConsole.MarkupLine($"[green]✓[/] Installed [bold]{skill.Name.EscapeMarkup()}[/] to [dim]{target.RelativePath}/{skill.Name}[/]");
    }

    private static void ShowSkillDetail(SkillInfo skill, bool isInstalled)
    {
        var panel = new Panel(new Rows(
            new Markup($"[bold]Name:[/]        {skill.Name.EscapeMarkup()}"),
            new Markup($"[bold]Description:[/] {skill.Description.EscapeMarkup()}"),
            new Markup($"[bold]Source:[/]      {skill.Source.EscapeMarkup()}"),
            new Markup($"[bold]Plugin:[/]      {(skill.PluginName ?? "—").EscapeMarkup()}"),
            new Markup($"[bold]License:[/]     {(skill.License ?? "—").EscapeMarkup()}"),
            new Markup($"[bold]Repo Path:[/]   {skill.FolderPath.EscapeMarkup()}"),
            new Markup($"[bold]Installed:[/]   {(isInstalled ? "[green]Yes[/]" : "[dim]No[/]")}")
        ))
        {
            Header = new PanelHeader($"[bold blue] {skill.Name.EscapeMarkup()} [/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
    }

    private static string TruncateDescription(string desc, int maxLen)
    {
        if (desc.Length <= maxLen) return desc;
        return desc[..(maxLen - 3)] + "...";
    }
}
