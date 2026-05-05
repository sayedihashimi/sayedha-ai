# dotnet-skills

A .NET 10 global CLI tool for browsing, installing, and managing Copilot/Claude skills and plugins in your .NET projects.

## Features

- **Browse** available .NET skills from curated sources
- **Search** by keyword across all skill catalogs
- **Install** skills into your repo with auto-detection of AI client folders
- **Update** installed skills to latest versions
- **Uninstall** skills you no longer need
- **Interactive TUI** powered by [Spectre.Console](https://spectreconsole.net/)
- **CLI subcommands** for scripting and CI scenarios

## Skill Sources

| Source | Description |
|--------|-------------|
| [dotnet/skills](https://github.com/dotnet/skills) | Official .NET agent skills (12 plugins covering core .NET, ASP.NET, MAUI, AI, testing, etc.) |
| [github/awesome-copilot](https://github.com/github/awesome-copilot) | Community-curated skills (.NET-related entries only) |

## Installation

```bash
dotnet tool install -g dotnet-skills
```

## Usage

### Interactive TUI

Launch the full interactive experience:

```bash
dotnet skills
```

### CLI Commands

```bash
# Browse all available skills
dotnet skills browse
dotnet skills browse --refresh    # Force refresh the catalog cache

# Search for skills
dotnet skills search <query>

# Install a skill
dotnet skills install <name>

# Update installed skills
dotnet skills update              # Update all
dotnet skills update <name>       # Update specific skill

# List installed skills
dotnet skills list

# Show skill details
dotnet skills info <name>

# Uninstall a skill
dotnet skills uninstall <name>
```

## How It Works

1. **Catalog**: Fetches skill metadata from source repos via the GitHub public API (no authentication required). Results are cached locally for 24 hours in `~/.dotnet-skills/cache/`.

2. **Install Target Detection**: When installing, the tool scans your repo for known AI client directories:
   - `.agents/skills/` (GitHub Copilot)
   - `.claude-plugin/` (Claude Code)
   - `.cursor-plugin/` (Cursor)
   
   If multiple are found, you'll be prompted to select which ones to install to. If none exist, it defaults to `.agents/skills/`.

3. **Update Detection**: Compares installed SKILL.md content hashes against remote versions to detect available updates.

## Requirements

- .NET 10 SDK or runtime
- Internet access for fetching skill catalogs from GitHub

## Building from Source

```bash
cd src/skills-cli
dotnet build
dotnet run --project src/DotnetSkills
```
