# Plan: `dotnet skills` CLI Tool

## Problem Statement

Create a .NET 10 global CLI tool (`dotnet-skills`) that lets developers interactively browse, install, update, uninstall, and manage Copilot/Claude skills and plugins in their current repo. The tool pulls from two curated sources (`dotnet/skills` and `github/awesome-copilot`, .NET entries only) and provides a rich interactive TUI powered by Spectre.Console.

## Proposed Approach

Build a .NET 10 console app packaged as a global tool. The architecture separates concerns into:
- **Catalog layer** — fetches and caches skill metadata from source repos via GitHub's public API (unauthenticated)
- **Installation layer** — downloads skill files and places them into the correct target folder(s) in the user's repo
- **TUI layer** — Spectre.Console-powered interactive menus, search, and multi-select
- **CLI layer** — subcommands for non-interactive / scripting use

## Key Design Decisions

| Decision | Choice |
|----------|--------|
| Distribution | .NET global tool (`dotnet tool install -g dotnet-skills`) |
| Source code location | `src/skills-cli/` in this repo |
| Target framework | .NET 10 |
| UI library | Spectre.Console |
| Source repos | `dotnet/skills` (marketplace.json + plugin.json), `github/awesome-copilot` (.NET-related entries only) |
| Source extensibility | Hardcoded sources; easy to add more in future code updates (not user-configurable) |
| GitHub auth | None — uses public unauthenticated API calls |
| Catalog caching | Local cache with 24-hour TTL |
| Install target detection | Auto-detect client folders (`.agents/skills/`, `.claude-plugin/`, etc.); prompt user to select if multiple exist; default to `.agents/skills/` if none found |
| Install granularity | Individual skills (SKILL.md + supporting files) |
| Tracking installs | Convention-based — presence of files in well-known directories |
| Testing | No automated tests for v1 |
| Interaction model | `dotnet skills` → full TUI; subcommands (`browse`, `install`, `list`, etc.) for scripting |

## Source Repo Structures

### dotnet/skills
```
plugins/<plugin-name>/
├── plugin.json           # { name, version, description, skills: ["./skills/"] }
├── skills/
│   ├── <skill-name>/
│   │   ├── SKILL.md      # YAML frontmatter (name, description) + workflow
│   │   ├── scripts/
│   │   ├── references/
│   │   └── assets/
```
- Top-level `marketplace.json` lists all plugins
- Each `plugin.json` has version, description, skills path
- SKILL.md has YAML frontmatter: `name`, `description`, `license`

### github/awesome-copilot
- Skills are individual folders under `skills/` in the repo root
- Each skill folder contains a `SKILL.md` with YAML frontmatter
- .NET-related skills can be identified by name/description containing: `dotnet`, `csharp`, `c#`, `.net`, `aspnet`, `asp.net`, `blazor`, `maui`, `ef-core`, `entity-framework`, `nuget`, `msbuild`, `aspire`, `fluentui-blazor`, `containerize-aspnet`, `containerize-aspnetcore`, `mstest`, `nunit`, `xunit`, `tunit`
- Also has agents, instructions, plugins, hooks, and workflows — we focus on skills and plugins only

## Commands

| Command | Interactive | Description |
|---------|------------|-------------|
| `dotnet skills` | Full TUI | Launch interactive menu with browse, search, install, manage |
| `dotnet skills browse` | Filterable list | Browse available skills with search/filter |
| `dotnet skills search <query>` | Table output | Search skills by keyword |
| `dotnet skills install <name>` | Confirmation prompt | Install a skill into the current repo |
| `dotnet skills update [name]` | Confirmation prompt | Update one or all installed skills |
| `dotnet skills uninstall <name>` | Confirmation prompt | Remove an installed skill |
| `dotnet skills list` | Table output | List currently installed skills |
| `dotnet skills info <name>` | Detail view | Show details about a specific skill |

## Project Structure

```
src/skills-cli/
├── DotnetSkills.sln
├── src/
│   └── DotnetSkills/
│       ├── DotnetSkills.csproj
│       ├── Program.cs                    # Entry point, command registration
│       ├── Commands/
│       │   ├── BrowseCommand.cs          # Browse available skills
│       │   ├── SearchCommand.cs          # Search by keyword
│       │   ├── InstallCommand.cs         # Install a skill
│       │   ├── UpdateCommand.cs          # Update installed skills
│       │   ├── UninstallCommand.cs       # Remove a skill
│       │   ├── ListCommand.cs            # List installed skills
│       │   └── InfoCommand.cs            # Show skill details
│       ├── Tui/
│       │   ├── MainMenu.cs              # Top-level interactive menu
│       │   ├── SkillBrowser.cs          # Interactive browse/search TUI
│       │   └── SkillInstaller.cs        # Interactive install flow with target selection
│       ├── Catalog/
│       │   ├── ICatalogSource.cs         # Interface for skill sources
│       │   ├── DotnetSkillsSource.cs     # Fetches from dotnet/skills
│       │   ├── AwesomeCopilotSource.cs   # Fetches from awesome-copilot (.NET only)
│       │   ├── CatalogManager.cs         # Aggregates sources, deduplicates
│       │   └── CatalogCache.cs           # Local JSON cache with TTL
│       ├── Models/
│       │   ├── SkillInfo.cs              # Skill metadata (name, description, source, files)
│       │   ├── PluginInfo.cs             # Plugin metadata (from plugin.json)
│       │   └── CatalogEntry.cs           # Unified catalog entry
│       ├── Installation/
│       │   ├── SkillInstaller.cs         # Downloads and places skill files
│       │   ├── TargetDetector.cs         # Detects client folders in repo
│       │   └── InstalledSkillTracker.cs  # Scans installed skills from file system
│       └── GitHub/
│           ├── GitHubApiClient.cs        # Unauthenticated GitHub REST API calls
│           └── GitHubContentFetcher.cs   # Fetches raw file content
```

## Implementation Todos

### Phase 1: Project scaffolding
- **scaffold-project**: Create .NET 10 console app, add Spectre.Console and System.CommandLine NuGet packages, configure as global tool in csproj
- **create-models**: Define `SkillInfo`, `PluginInfo`, `CatalogEntry` model classes

### Phase 2: Catalog / data layer
- **github-api-client**: Implement `GitHubApiClient` for unauthenticated REST calls (get directory listing, get file content, get raw file)
- **dotnet-skills-source**: Implement `DotnetSkillsSource` — parse `marketplace.json`, iterate plugins, read `plugin.json` and SKILL.md frontmatter
- **awesome-copilot-source**: Implement `AwesomeCopilotSource` — list `skills/` directory, filter .NET-related entries, parse SKILL.md frontmatter
- **catalog-manager**: Implement `CatalogManager` — aggregate sources, deduplicate, provide search/filter
- **catalog-cache**: Implement `CatalogCache` — JSON file cache in `~/.dotnet-skills/cache/` with 24-hour TTL

### Phase 3: Installation layer
- **target-detector**: Implement `TargetDetector` — scan current repo for `.agents/skills/`, `.claude-plugin/`, `.cursor-plugin/`, etc.
- **skill-installer**: Implement `SkillInstaller` — download SKILL.md + supporting files from GitHub, write to target folder
- **installed-tracker**: Implement `InstalledSkillTracker` — scan target folders, parse installed SKILL.md files, compare with catalog for update detection

### Phase 4: CLI commands
- **browse-command**: `dotnet skills browse` with Spectre table output
- **search-command**: `dotnet skills search <query>` with keyword matching
- **install-command**: `dotnet skills install <name>` with target folder selection
- **update-command**: `dotnet skills update [name]` with version comparison
- **uninstall-command**: `dotnet skills uninstall <name>` with confirmation
- **list-command**: `dotnet skills list` showing installed skills
- **info-command**: `dotnet skills info <name>` showing full details

### Phase 5: Interactive TUI
- **main-menu**: Full-screen TUI main menu (Browse, Search, Manage Installed, Exit)
- **skill-browser-tui**: Interactive skill browser with arrow-key navigation, search-as-you-type, multi-select for batch install
- **install-flow-tui**: Interactive install flow — select skills → detect targets → confirm → download → report

### Phase 6: Polish & packaging
- **global-tool-packaging**: Configure csproj for `dotnet tool install -g dotnet-skills`, set tool command name
- **error-handling**: Add graceful error handling for network failures, missing repos, invalid cache
- **readme**: Create README.md for the CLI tool

## Notes & Considerations

1. **Rate limiting**: Unauthenticated GitHub API allows 60 requests/hour. The 24-hour cache mitigates this, but first-run or `--refresh` could hit limits if catalogs are large. Consider fetching `marketplace.json` first (1 request) then expanding lazily.

2. **.NET filtering for awesome-copilot**: Use keyword matching on skill names and descriptions. Known .NET skill prefixes from awesome-copilot: `aspire`, `aspnet-minimal-api-openapi`, `containerize-aspnet-framework`, `containerize-aspnetcore`, `csharp-async`, `csharp-docs`, `csharp-mcp-server-generator`, `csharp-mstest`, `csharp-nunit`, `csharp-tunit`, `csharp-xunit`, `dotnet-best-practices`, `dotnet-design-pattern-review`, `dotnet-timezone`, `dotnet-upgrade`, `ef-core`, `fluentui-blazor`.

3. **Install target auto-detection**: The tool scans the repo root for known directories. If multiple are found (e.g., both `.agents/skills/` and `.claude-plugin/`), prompt with multi-select. If none found, default to `.agents/skills/` and create it.

4. **Update detection**: Compare installed SKILL.md content hash against the remote version. No version numbering needed — hash comparison is sufficient.

5. **Spectre.Console features to leverage**: `SelectionPrompt`, `MultiSelectionPrompt`, `TextPrompt`, `Table`, `Panel`, `Tree`, `Status` (spinners), `Live` display for progress.

6. **Future source repos**: The `ICatalogSource` interface makes it straightforward to add new sources by implementing a new class and registering it in `CatalogManager`.
