# sayedha-ai — Copilot Completion Notification

Reusable [GitHub Copilot custom instruction](https://code.visualstudio.com/docs/copilot/customization/custom-instructions) that plays a **sound** and shows a **Windows notification** when Copilot finishes a task.

## What It Does

When installed in a repo, Copilot will automatically — at the very end of every task:

1. 🎵 **Play a completion melody** — a short ascending C-major jingle (C5→D5→E5→G5)
2. 🔔 **Show a Windows balloon notification** — "GitHub Copilot — Task completed!"

This helps you notice when Copilot is done, especially during long-running agent tasks where you've switched to another window.

## Quick Start

### Install into any repo

```powershell
# Clone this repo (one-time setup)
git clone https://github.com/sayedihashimi/sayedha-ai.git

# Install into your project
.\sayedha-ai\Install-CopilotInstruction.ps1 -TargetRepo C:\path\to\your\project
```

This copies two files into your project:

| File | Purpose |
|------|---------|
| `.github/instructions/notify-on-completion.instructions.md` | Copilot instruction (auto-applied to all files) |
| `.github/scripts/copilot-notify.ps1` | Notification script (sound + popup) |

Commit the `.github/` files to your repo and you're done.

### Uninstall

```powershell
.\sayedha-ai\Uninstall-CopilotInstruction.ps1 -TargetRepo C:\path\to\your\project
```

### Test the notification manually

```powershell
pwsh -NoProfile -File .github/scripts/copilot-notify.ps1
```

## Requirements

- **Windows** (uses `System.Console.Beep` and `System.Windows.Forms.NotifyIcon`)
- **PowerShell 7+** (PowerShell Core)
- **VS Code** with GitHub Copilot in **agent mode** (Copilot must be able to execute terminal commands)

## How It Works

The instruction file (`.github/instructions/notify-on-completion.instructions.md`) uses the `applyTo: "**"` frontmatter to automatically apply to all Copilot interactions. It tells Copilot to run the notification script as its very last action.

The notification script (`copilot-notify.ps1`) uses only built-in .NET APIs — no external PowerShell modules or dependencies required.

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

## Limitations

- **Agent mode only** — The notification only works when Copilot is in agent mode (where it can execute terminal commands). In regular chat mode, Copilot generates text but doesn't run commands.
- **Windows only** — The notification APIs are Windows-specific. macOS/Linux support could be added in the future.
- **Not a guarantee** — Copilot follows instructions on a best-effort basis. It will usually run the notification, but it's not 100% guaranteed on every single interaction.

## File Structure

```
sayedha-ai/
├── README.md                          # This file
├── instructions/
│   └── notify-on-completion.instructions.md   # Copilot instruction (source)
├── scripts/
│   └── copilot-notify.ps1            # Notification script (source)
├── Install-CopilotInstruction.ps1    # Install into any repo
├── Uninstall-CopilotInstruction.ps1  # Clean removal
└── LICENSE
```

## License

See [LICENSE](LICENSE) for details.
