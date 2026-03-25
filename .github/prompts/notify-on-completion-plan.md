# Plan: Reusable Copilot "Completion Notification" Instruction

## Problem
Create a reusable AI artifact that instructs VS Code Copilot to play a completion sound AND show a Windows toast notification at the end of every task. It should be easy to install into any repo with minimal friction.

## Key Research Findings
- **No user-level instructions file exists** — VS Code Copilot does NOT support a global user-level `.copilot-instructions.md` file or absolute path file references in settings.
- **Per-repo instructions** (`.github/copilot-instructions.md`) are automatically included in every Copilot Chat request for that repo — this is the best mechanism for "always on" behavior.
- **Best approach**: Store the instruction content in this repo, provide a PowerShell install script to copy it into any target repo's `.github/copilot-instructions.md`.

### Sound & Notification Options (Tested & Working in PowerShell 7 / PS Core)
- **Multi-tone melody**: `[System.Console]::Beep(freq, duration)` with multiple tones (C-D-E-G ascending jingle) — more pleasant than a single beep ✓
- **System sounds**: `[System.Media.SystemSounds]::Asterisk.Play()` — plays the Windows "information" sound ✓
- **Windows balloon notification**: `System.Windows.Forms.NotifyIcon.ShowBalloonTip()` — shows a native Windows notification popup. Works in **PowerShell 7** (no Windows PowerShell 5.1 needed) ✓

## Approach
Script-based install pattern with **sound + toast notification**:
1. Store the canonical instruction as a markdown file in this repo
2. Also store the notification PowerShell script that Copilot will invoke
3. Provide an install script to deploy both into any target repo
4. Document usage in README.md

## Todos

### 1. Create the notification script (`scripts/copilot-notify.ps1`)
- Standalone PowerShell (Core / PS7) script that:
  - Plays a pleasant multi-tone completion melody (C5-D5-E5-G5 ascending)
  - Shows a Windows balloon notification ("GitHub Copilot — Task completed!") using `System.Windows.Forms.NotifyIcon`
  - All pure .NET — no Windows PowerShell 5.1 or external modules needed
  - Accepts optional `-Message` parameter to customize the notification text
  - Non-blocking (doesn't prevent Copilot from continuing)

### 2. Create the Copilot instruction content (`instructions/notify-on-completion.instructions.md`)
- Markdown file with frontmatter `applyTo: "**"` so it applies to all files
- Clear Copilot instructions to run the notification script at the end of every task
- References the script by relative path
- Keeps it concise to minimize context usage

### 3. Create the install script (`Install-CopilotInstruction.ps1`)
- PowerShell script that:
  - Accepts a target repo path (defaults to current directory)
  - Creates `.github/instructions/` directory if it doesn't exist
  - Copies the instruction file to `.github/instructions/notify-on-completion.instructions.md`
  - Copies the notification script to `.github/scripts/copilot-notify.ps1`
  - Detects and avoids duplicates (using marker comments)
  - Provides clear output about what was done

### 4. Create an uninstall script (`Uninstall-CopilotInstruction.ps1`)
- Removes the installed instruction file and notification script from a target repo
- Cleans up empty directories if nothing remains

### 5. Create README.md
- Explains what this repo does
- Quick start / usage instructions
- How the notification works (sound + toast)
- Requirements (Windows, VS Code Copilot agent mode)
- Notes about limitations

## File Structure
```
sayedha-ai/
├── README.md
├── instructions/
│   └── notify-on-completion.instructions.md   # Copilot instruction (with applyTo frontmatter)
├── scripts/
│   └── copilot-notify.ps1                     # Notification script (sound + toast)
├── Install-CopilotInstruction.ps1             # Install into any repo
├── Uninstall-CopilotInstruction.ps1           # Clean uninstall
└── LICENSE
```

## Design Decisions
- **Using `.github/instructions/*.instructions.md`** with `applyTo: "**"` instead of `copilot-instructions.md` — this is the newer, recommended pattern and allows coexistence with other instructions without merge conflicts.
- **Separate notification script** rather than inline PowerShell in the instruction — keeps the instruction concise and the script testable/customizable.
- **Pure PowerShell Core (PS7)** — all .NET APIs used (`System.Console.Beep`, `System.Windows.Forms.NotifyIcon`) work in PS7. No dependency on Windows PowerShell 5.1 or external modules.
- **Multi-tone melody** (C5→D5→E5→G5) — more recognizable and pleasant than a single beep, ~750ms total duration.

## Additional Requirements
- Save a copy of this plan to `.github/prompts/notify-on-completion-plan.md` in the repo
- Make git commits as work progresses
- Test the notification script and install/uninstall scripts to verify they work

## Notes
- The instruction only works in VS Code Copilot **agent mode** (where Copilot can execute terminal commands). In regular chat mode, Copilot generates text but doesn't run commands.
- Windows-focused for now. macOS/Linux support could be added later (using `afplay`/`paplay` for sound, `osascript`/`notify-send` for notifications).
