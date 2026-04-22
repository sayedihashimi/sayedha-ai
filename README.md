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

## Plugins

### [copilot-notify](plugins/copilot-notify/)

A Copilot CLI / Claude Code **plugin** that plays a completion sound and shows a Windows notification when a session ends or a task completes. Uses **hooks** (`Stop` and `SessionEnd` events) instead of custom instructions — more reliable because hooks are enforced by the runtime, not by LLM instruction-following.

**Install:**

```powershell
# Copilot CLI
copilot plugin install sayedihashimi/sayedha-ai:plugins/copilot-notify

# Claude Code
claude --plugin-dir ./plugins/copilot-notify
```

See the [plugin README](plugins/copilot-notify/README.md) for full details.

---

## Custom Instructions (Legacy)

### Copilot Completion Notification (deprecated)

> **Note:** The custom instruction approach (`instructions/notify-on-completion.instructions.md`) is deprecated in favor of the [copilot-notify plugin](plugins/copilot-notify/) above. The plugin uses lifecycle hooks which are more reliable than relying on the LLM to run a script after every response.

The legacy install/uninstall scripts (`Install-CopilotInstruction.ps1`, `Uninstall-CopilotInstruction.ps1`) still work for the custom instruction approach if needed.

## File Structure

```
sayedha-ai/
├── README.md
├── .agents/
│   └── skills/
│       ├── dotnet-new-template/           # .NET template authoring skill
│       │   ├── SKILL.md
│       │   ├── reference/
│       │   ├── examples/
│       │   ├── templates/
│       │   └── scripts/
│       └── dotnet-watch/                  # .NET app runner with hot-reload
│           ├── SKILL.md
│           ├── reference/
│           └── examples/
├── plugins/
│   └── copilot-notify/                   # Session notification plugin
│       ├── .claude-plugin/plugin.json
│       ├── .github/plugin/plugin.json
│       ├── hooks/hooks.json
│       ├── scripts/
│       │   ├── copilot-notify.ps1
│       │   └── copilot-notify.wav
│       ├── README.md
│       └── LICENSE
├── instructions/                          # Legacy custom instructions
│   └── notify-on-completion.instructions.md
├── scripts/                               # Legacy scripts
│   └── copilot-notify.ps1
├── Install-CopilotInstruction.ps1         # Legacy install script
├── Uninstall-CopilotInstruction.ps1       # Legacy uninstall script
└── LICENSE
```

## License

See [LICENSE](LICENSE) for details.
