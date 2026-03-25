# sayedha-ai — Copilot Completion Notification

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
├── instructions/
│   └── notify-on-completion.instructions.md   # Copilot instruction (per-repo source)
├── scripts/
│   └── copilot-notify.ps1                     # Notification script (source)
├── Install-CopilotInstruction.ps1             # Install (global or per-repo)
├── Uninstall-CopilotInstruction.ps1           # Uninstall
└── LICENSE
```

## License

See [LICENSE](LICENSE) for details.
