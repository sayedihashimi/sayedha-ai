# copilot-notify

A Copilot CLI / Claude Code **plugin** that plays a completion sound and shows a Windows notification when a session ends or a task completes.

## What It Does

When installed as a plugin, two hooks fire automatically — no custom instructions needed:

| Hook Event | When It Fires | Notification |
|------------|---------------|--------------|
| `Stop` | After each Copilot/Claude response | "Task completed" |
| `SessionEnd` | When the session terminates | "Session ended" |

Each hook:

1. 🎵 **Plays a completion melody** — a WAV jingle (or fallback system beeps)
2. 🔔 **Shows a Windows balloon notification** in the system tray

## Installation

### From this repository (local)

```powershell
# Copilot CLI
copilot plugin install ./plugins/copilot-notify

# Claude Code
claude --plugin-dir ./plugins/copilot-notify
```

### From GitHub

```powershell
# Copilot CLI
copilot plugin install sayedihashimi/sayedha-ai:plugins/copilot-notify

# Claude Code — add to .claude/settings.json:
# { "plugins": ["sayedihashimi/sayedha-ai:plugins/copilot-notify"] }
```

## Uninstall

```powershell
# Copilot CLI
copilot plugin uninstall copilot-notify
```

## Requirements

- **Windows** (uses `System.Console.Beep` and `System.Windows.Forms.NotifyIcon`)
- **PowerShell 7+** (PowerShell Core)

## Customization

The notification script accepts parameters. To customize, edit the hook commands in `hooks/hooks.json`:

```json
{
  "type": "command",
  "command": "pwsh -NoProfile -File \"${CLAUDE_PLUGIN_ROOT}/scripts/copilot-notify.ps1\" -Title \"My Project\" -Message \"Done!\" -NoSound"
}
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `-Title` | `"Copilot"` | Notification title |
| `-Message` | `"Task completed!"` | Notification body |
| `-NoSound` | off | Skip the melody, notification only |
| `-NoNotification` | off | Skip the popup, sound only |

## How It Works

Unlike the previous custom-instruction approach (which relied on the LLM remembering to run a script after every response), this plugin uses **hooks** — event handlers that fire automatically on lifecycle events. This is more reliable because:

- Hooks are enforced by the runtime, not by LLM instruction-following
- No context tokens are consumed (hooks don't load into the prompt)
- No risk of the notification being skipped or forgotten

## File Structure

```
copilot-notify/
├── .claude-plugin/
│   └── plugin.json          # Plugin manifest
├── .github/
│   └── plugin/
│       └── plugin.json      # Copilot CLI manifest (cross-platform)
├── hooks/
│   └── hooks.json           # SessionEnd + Stop hooks
├── scripts/
│   ├── copilot-notify.ps1   # Notification script
│   └── copilot-notify.wav   # Optional completion jingle
├── README.md
└── LICENSE
```

## License

MIT — see [LICENSE](LICENSE).
