using DotnetSkills.Catalog;
using DotnetSkills.Installation;
using Spectre.Console;

namespace DotnetSkills.Commands;

public class UpdateCommand
{
    private readonly CatalogCache _cache;
    private readonly SkillInstaller _installer;
    private readonly InstalledSkillTracker _tracker;

    public UpdateCommand(CatalogCache cache, SkillInstaller installer, InstalledSkillTracker tracker)
    {
        _cache = cache;
        _installer = installer;
        _tracker = tracker;
    }

    public async Task ExecuteAsync(string? name = null, CancellationToken ct = default)
    {
        var installed = _tracker.GetInstalledSkills();
        if (installed.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No skills are currently installed.[/]");
            return;
        }

        if (name != null)
        {
            await UpdateSingleAsync(name, installed, ct);
        }
        else
        {
            await UpdateAllAsync(installed, ct);
        }
    }

    private async Task UpdateSingleAsync(string name, List<InstalledSkill> installed, CancellationToken ct)
    {
        var installedSkill = installed.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (installedSkill == null)
        {
            AnsiConsole.MarkupLine($"[red]Skill '[bold]{name.EscapeMarkup()}[/]' is not installed.[/]");
            return;
        }

        var catalogSkill = await _cache.FindByNameAsync(name, forceRefresh: true, ct: ct);
        if (catalogSkill == null)
        {
            AnsiConsole.MarkupLine($"[red]Skill '[bold]{name.EscapeMarkup()}[/]' not found in catalog.[/]");
            return;
        }

        if (catalogSkill.ContentHash == installedSkill.ContentHash)
        {
            AnsiConsole.MarkupLine($"[green]'{name}' is already up to date.[/]");
            return;
        }

        var confirm = AnsiConsole.Confirm($"Update [bold]{name}[/]?");
        if (!confirm) return;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Updating {name}...", async _ =>
            {
                await _installer.UpdateAsync(catalogSkill, installedSkill.InstalledPath, ct);
            });

        AnsiConsole.MarkupLine($"[green]✓[/] Updated [bold]{name.EscapeMarkup()}[/]");
    }

    private async Task UpdateAllAsync(List<InstalledSkill> installed, CancellationToken ct)
    {
        AnsiConsole.MarkupLine("[dim]Checking for updates...[/]");
        var updatable = new List<(InstalledSkill Installed, Models.SkillInfo Catalog)>();

        foreach (var skill in installed)
        {
            var catalogSkill = await _cache.FindByNameAsync(skill.Name, forceRefresh: true, ct: ct);
            if (catalogSkill != null && catalogSkill.ContentHash != skill.ContentHash)
            {
                updatable.Add((skill, catalogSkill));
            }
        }

        if (updatable.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All skills are up to date.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[yellow]{updatable.Count} skill(s) have updates available:[/]");
        foreach (var (inst, _) in updatable)
        {
            AnsiConsole.MarkupLine($"  • {inst.Name}");
        }

        var confirm = AnsiConsole.Confirm("Update all?");
        if (!confirm) return;

        foreach (var (inst, cat) in updatable)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Updating {inst.Name}...", async _ =>
                {
                    await _installer.UpdateAsync(cat, inst.InstalledPath, ct);
                });

            AnsiConsole.MarkupLine($"[green]✓[/] Updated [bold]{inst.Name.EscapeMarkup()}[/]");
        }
    }
}
