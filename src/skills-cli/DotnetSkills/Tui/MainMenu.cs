using DotnetSkills.Catalog;
using DotnetSkills.Commands;
using DotnetSkills.Installation;
using Spectre.Console;

namespace DotnetSkills.Tui;

public class MainMenu
{
    private readonly CatalogCache _cache;
    private readonly InstalledSkillTracker _tracker;
    private readonly SkillInstaller _installer;
    private readonly TargetDetector _detector;
    private readonly string _repoRoot;

    public MainMenu(CatalogCache cache, InstalledSkillTracker tracker, SkillInstaller installer, TargetDetector detector, string repoRoot)
    {
        _cache = cache;
        _tracker = tracker;
        _installer = installer;
        _detector = detector;
        _repoRoot = repoRoot;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        AnsiConsole.Write(new FigletText("dotnet skills").Color(Color.Blue));
        AnsiConsole.MarkupLine("[dim]Browse, install, and manage skills and plugins for .NET[/]\n");

        while (!ct.IsCancellationRequested)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What would you like to do?[/]")
                    .HighlightStyle(new Style(Color.Cyan1))
                    .AddChoices(
                    [
                        "🔍 Browse available skills",
                        "🔎 Search skills",
                        "📦 Install a skill",
                        "📋 List installed skills",
                        "🔄 Update skills",
                        "🗑️  Uninstall a skill",
                        "ℹ️  Skill details",
                        "🚪 Exit"
                    ]));

            switch (choice)
            {
                case "🔍 Browse available skills":
                    await new SkillBrowser(_cache, _tracker, _installer, _detector, _repoRoot).BrowseAsync(ct);
                    break;

                case "🔎 Search skills":
                    var query = AnsiConsole.Ask<string>("[bold]Search query:[/]");
                    await new SearchCommand(_cache, _tracker).ExecuteAsync(query, ct);
                    break;

                case "📦 Install a skill":
                    await new SkillBrowser(_cache, _tracker, _installer, _detector, _repoRoot).InstallFlowAsync(ct);
                    break;

                case "📋 List installed skills":
                    await new ListCommand(_tracker).ExecuteAsync(ct);
                    break;

                case "🔄 Update skills":
                    await new UpdateCommand(_cache, _installer, _tracker).ExecuteAsync(ct: ct);
                    break;

                case "🗑️  Uninstall a skill":
                    await UninstallFlowAsync(ct);
                    break;

                case "ℹ️  Skill details":
                    await InfoFlowAsync(ct);
                    break;

                case "🚪 Exit":
                    return;
            }

            AnsiConsole.WriteLine();
        }
    }

    private async Task UninstallFlowAsync(CancellationToken ct)
    {
        var installed = _tracker.GetInstalledSkills();
        if (installed.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No skills are currently installed.[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a skill to uninstall:")
                .AddChoices(installed.Select(s => s.Name)));

        await new UninstallCommand(_tracker).ExecuteAsync(selected, ct);
    }

    private async Task InfoFlowAsync(CancellationToken ct)
    {
        var name = AnsiConsole.Ask<string>("[bold]Skill name:[/]");
        await new InfoCommand(_cache, _tracker).ExecuteAsync(name, ct);
    }
}
