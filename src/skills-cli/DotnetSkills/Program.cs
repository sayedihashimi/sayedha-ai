using System.CommandLine;
using DotnetSkills.Catalog;
using DotnetSkills.Commands;
using DotnetSkills.GitHub;
using DotnetSkills.Installation;
using DotnetSkills.Tui;

var repoRoot = TargetDetector.FindRepoRoot(Directory.GetCurrentDirectory());
var gitHubClient = new GitHubApiClient();
var fetcher = new GitHubContentFetcher(gitHubClient);

var catalogManager = new CatalogManager([
    new DotnetSkillsSource(gitHubClient),
    new AwesomeCopilotSource(gitHubClient)
]);
var cache = new CatalogCache(catalogManager);
var detector = new TargetDetector(repoRoot);
var installer = new SkillInstaller(fetcher);
var tracker = new InstalledSkillTracker(detector);

var rootCommand = new RootCommand("Browse, install, and manage skills and plugins for .NET projects");

// No subcommand → launch TUI
rootCommand.SetAction(async (ctx, ct) =>
{
    var menu = new MainMenu(cache, tracker, installer, detector, repoRoot);
    await menu.RunAsync(ct);
});

// browse
var browseCommand = new Command("browse", "Browse available skills");
var refreshOption = new Option<bool>("--refresh") { Description = "Force refresh the catalog cache" };
browseCommand.Add(refreshOption);
browseCommand.SetAction(async (ctx, ct) =>
{
    var refresh = ctx.GetValue(refreshOption);
    await new BrowseCommand(cache, tracker).ExecuteAsync(refresh, ct);
});
rootCommand.Add(browseCommand);

// search
var searchCommand = new Command("search", "Search for skills by keyword");
var queryArg = new Argument<string>("query") { Description = "Search query" };
searchCommand.Add(queryArg);
searchCommand.SetAction(async (ctx, ct) =>
{
    var query = ctx.GetValue(queryArg)!;
    await new SearchCommand(cache, tracker).ExecuteAsync(query, ct);
});
rootCommand.Add(searchCommand);

// install
var installCommand = new Command("install", "Install a skill into the current repo");
var installNameArg = new Argument<string>("name") { Description = "Skill name to install" };
installCommand.Add(installNameArg);
installCommand.SetAction(async (ctx, ct) =>
{
    var name = ctx.GetValue(installNameArg)!;
    await new InstallCommand(cache, installer, detector, tracker).ExecuteAsync(name, ct);
});
rootCommand.Add(installCommand);

// update
var updateCommand = new Command("update", "Update installed skills");
var updateNameArg = new Argument<string?>("name") { Description = "Skill name to update (omit for all)", DefaultValueFactory = _ => null };
updateCommand.Add(updateNameArg);
updateCommand.SetAction(async (ctx, ct) =>
{
    var name = ctx.GetValue(updateNameArg);
    await new UpdateCommand(cache, installer, tracker).ExecuteAsync(name, ct);
});
rootCommand.Add(updateCommand);

// uninstall
var uninstallCommand = new Command("uninstall", "Uninstall a skill");
var uninstallNameArg = new Argument<string>("name") { Description = "Skill name to uninstall" };
uninstallCommand.Add(uninstallNameArg);
uninstallCommand.SetAction(async (ctx, ct) =>
{
    var name = ctx.GetValue(uninstallNameArg)!;
    await new UninstallCommand(tracker).ExecuteAsync(name, ct);
});
rootCommand.Add(uninstallCommand);

// list
var listCommand = new Command("list", "List installed skills");
listCommand.SetAction(async (ctx, ct) =>
{
    await new ListCommand(tracker).ExecuteAsync(ct);
});
rootCommand.Add(listCommand);

// info
var infoCommand = new Command("info", "Show details about a skill");
var infoNameArg = new Argument<string>("name") { Description = "Skill name" };
infoCommand.Add(infoNameArg);
infoCommand.SetAction(async (ctx, ct) =>
{
    var name = ctx.GetValue(infoNameArg)!;
    await new InfoCommand(cache, tracker).ExecuteAsync(name, ct);
});
rootCommand.Add(infoCommand);

return await rootCommand.Parse(args).InvokeAsync();
