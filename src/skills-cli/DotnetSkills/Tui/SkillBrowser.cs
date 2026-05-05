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
    private readonly string _repoRoot;

    public SkillBrowser(CatalogCache cache, InstalledSkillTracker tracker, SkillInstaller installer, TargetDetector detector, string repoRoot)
    {
        _cache = cache;
        _tracker = tracker;
        _installer = installer;
        _detector = detector;
        _repoRoot = repoRoot;
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

        // Detect project profile and show recommended skills first
        var projectDetector = new ProjectDetector(_repoRoot);
        var profile = projectDetector.DetectProfile();

        if (profile.HasAnyDetection)
        {
            var recommended = projectDetector.GetRecommendedSkills(skills, profile);
            if (recommended.Count > 0)
            {
                AnsiConsole.MarkupLine($"[bold]Detected project types:[/] [cyan]{profile.GetSummary()}[/]\n");

                // Show recommended list directly with "Browse all" as last option
                var browseAllSentinel = new SkillInfo
                {
                    Name = "📋 Browse all skills...",
                    Description = "",
                    Source = "",
                    RepoOwner = "",
                    RepoName = "",
                    FolderPath = ""
                };

                var choices = recommended.Append(browseAllSentinel).ToList();
                var selected = AnsiConsole.Prompt(
                    new SelectionPrompt<SkillInfo>()
                        .Title($"[bold]⭐ Recommended skills[/] [dim](type to filter, enter to select)[/]")
                        .PageSize(15)
                        .EnableSearch()
                        .HighlightStyle(new Style(Color.Cyan1))
                        .AddChoices(choices)
                        .UseConverter(s =>
                        {
                            if (s == browseAllSentinel)
                                return "📋 Browse all skills...";
                            var status = installedNames.Contains(s.Name) ? " ✓" : "";
                            var plugin = s.PluginName != null ? $" ({s.PluginName})" : "";
                            return $"{s.Name}{status}{plugin} - {TruncateDescription(s.Description, 60)}";
                        }));

                if (selected != browseAllSentinel)
                {
                    await ShowAndOfferInstall(selected, installedNames, ct);
                    return;
                }
                // Fall through to browse all
            }
        }

        // Browse all skills
        var allSelected = PromptSkillSelection(skills, installedNames,
            "[bold]Browse skills[/] [dim](type to filter, enter to view details)[/]");
        if (allSelected != null)
        {
            await ShowAndOfferInstall(allSelected, installedNames, ct);
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

    private static SkillInfo? PromptSkillSelection(List<SkillInfo> skills, HashSet<string> installedNames, string title)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<SkillInfo>()
                .Title(title)
                .PageSize(15)
                .EnableSearch()
                .HighlightStyle(new Style(Color.Cyan1))
                .AddChoices(skills)
                .UseConverter(s =>
                {
                    var status = installedNames.Contains(s.Name) ? " ✓" : "";
                    var plugin = s.PluginName != null ? $" ({s.PluginName})" : "";
                    return $"{s.Name}{status}{plugin} - {TruncateDescription(s.Description, 60)}";
                }));
    }

    private async Task ShowAndOfferInstall(SkillInfo selected, HashSet<string> installedNames, CancellationToken ct)
    {
        ShowSkillDetail(selected, installedNames.Contains(selected.Name));

        if (!installedNames.Contains(selected.Name))
        {
            var install = AnsiConsole.Confirm("Install this skill?", defaultValue: true);
            if (install)
            {
                await InstallSkillAsync(selected, ct);
            }
        }
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
