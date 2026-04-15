# sayedha-ai — Copilot Tools & Skills

This repository contains reusable GitHub Copilot customizations: **custom instructions** and **agent skills**.

## Skills

### [dotnet-new-template](.agents/skills/dotnet-new-template/SKILL.md)

A Copilot agent skill for creating .NET project, item, and solution templates. Guides you through creating `template.json`, `ide.host.json`, configuring parameters/symbols, and packaging templates as NuGet packages. Auto-detects existing .NET projects and offers to convert them into reusable templates.

**Features:**
- Create project, item, and solution templates for `dotnet new` and Visual Studio
- Auto-detect existing .NET projects in the working directory
- Generate `template.json` with proper symbols, parameters, and conditional content
- Generate `ide.host.json` for Visual Studio integration
- Validate templates with the included PowerShell linting script
- NuGet packaging guidance for template distribution

### [dotnet-watch](.agents/skills/dotnet-watch/SKILL.md)

A Copilot agent skill for running .NET applications with `dotnet watch --non-interactive` instead of `dotnet run`. Avoids file-lock issues and enables automatic hot-reload when source files change, so the app stays running across code edits without manual restarts.

**Features:**
- Use `dotnet watch --non-interactive` for all .NET runnable project types
- Monitor console output for hot-reload success, build errors, and rude edits
- Automatic app restart on rude edits (no manual intervention)
- Guidance for editing code while the app is running

---

## Copilot Completion Notification

Reusable [GitHub Copilot custom instruction](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-custom-instructions) that plays a **sound** and shows a **Windows notification** when Copilot finishes a task.

## What It Does

When installed, Copilot will automatically — at the very end of every task:

1. 🎵 **Play a completion melody** — a short ascending C-major jingle (C5→D5→E5→G5)
2. 🔔 **Show a Windows balloon notification** — "GitHub Copilot — Task completed!"

This helps you notice when Copilot is done, especially during long-running agent tasks where you've switched to another window.

## Quick Start

### Global Install (Recommended — applies to all repos)

```powershell
# Clone this repo (one-time)
git clone https://github.com/sayedihashimi/sayedha-ai.git

# Install globally
.\sayedha-ai\Install-CopilotInstruction.ps1
```

This installs to `~/.copilot/`, which the Copilot CLI reads on **every session across all repos**:

| File | Purpose |
|------|---------|
| `~/.copilot/copilot-instructions.md` | Appends the notification instruction |
| `~/.copilot/scripts/copilot-notify.ps1` | Notification script (sound + popup) |

No per-repo setup needed. No files to commit.

### Per-Repo Install (alternative)

```powershell
.\sayedha-ai\Install-CopilotInstruction.ps1 -TargetRepo C:\path\to\your\project
```

This copies files into the repo's `.github/` directory. Commit them to share with collaborators.

### Uninstall

```powershell
# Remove global install
.\sayedha-ai\Uninstall-CopilotInstruction.ps1

# Remove from a specific repo
.\sayedha-ai\Uninstall-CopilotInstruction.ps1 -TargetRepo C:\path\to\your\project
```

### Test the notification manually

```powershell
pwsh -NoProfile -File ~/.copilot/scripts/copilot-notify.ps1
```

## Requirements

- **Windows** (uses `System.Console.Beep` and `System.Windows.Forms.NotifyIcon`)
- **PowerShell 7+** (PowerShell Core)
- **GitHub Copilot CLI** or **VS Code Copilot** in agent mode

## How It Works

### Copilot CLI (primary)

The Copilot CLI reads `$HOME/.copilot/copilot-instructions.md` on every session. The install script appends a notification instruction block (wrapped in marker comments for clean install/uninstall). It also reads `.github/instructions/*.instructions.md` from the current repo.

### VS Code Copilot

The per-repo install uses `.github/instructions/notify-on-completion.instructions.md` with `applyTo: "**"` frontmatter, which VS Code Copilot picks up automatically.

### Customization

The notification script accepts parameters:

```powershell
# Custom message and title
.\copilot-notify.ps1 -Message "Build succeeded!" -Title "My Project"

# Sound only (no popup)
.\copilot-notify.ps1 -NoNotification

# Notification only (no sound)
.\copilot-notify.ps1 -NoSound
```

## Instruction Locations (Copilot CLI)

The Copilot CLI reads instructions from these locations (all are supported):

| Location | Scope |
|----------|-------|
| `$HOME/.copilot/copilot-instructions.md` | **User-global** (all repos) |
| `.github/copilot-instructions.md` | Per-repo |
| `.github/instructions/**/*.instructions.md` | Per-repo (path-specific) |
| `AGENTS.md` | Per-repo |
| `COPILOT_CUSTOM_INSTRUCTIONS_DIRS` env var | Custom directories |

## Limitations

- **Agent mode only** — The notification only works when Copilot is in agent mode (where it can execute terminal commands). In regular chat, Copilot generates text but doesn't run commands.
- **Windows only** — The notification APIs are Windows-specific. macOS/Linux support could be added in the future.
- **Best-effort** — Copilot follows instructions on a best-effort basis. It will usually run the notification, but it's not guaranteed on every single interaction.

## File Structure

```
sayedha-ai/
├── README.md
├── .agents/
│   └── skills/
│       ├── dotnet-new-template/           # .NET template authoring skill
│       │   ├── SKILL.md                   # Skill entry point
│       │   ├── reference/                 # Detailed reference docs
│       │   ├── examples/                  # Golden input/output examples
│       │   ├── templates/                 # Starter JSON templates
│       │   └── scripts/                   # Validation script
│       └── dotnet-watch/                  # .NET app runner with hot-reload
│           ├── SKILL.md                   # Skill entry point
│           ├── reference/                 # Console output patterns
│           └── examples/                  # Workflow examples
├── instructions/
│   └── notify-on-completion.instructions.md
├── scripts/
│   └── copilot-notify.ps1
├── Install-CopilotInstruction.ps1
├── Uninstall-CopilotInstruction.ps1
└── LICENSE
```

## License

See [LICENSE](LICENSE) for details.
