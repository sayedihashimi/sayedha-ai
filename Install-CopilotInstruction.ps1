<#
.SYNOPSIS
    Installs the Copilot completion notification globally or into a specific repository.

.DESCRIPTION
    Installs a Copilot instruction and notification script so that Copilot automatically
    plays a sound and shows a Windows notification when it completes a task.

    By default, installs globally to ~/.copilot/ so it applies to ALL Copilot CLI
    sessions across all repositories. Use -TargetRepo to install per-repo instead.

    Global install (default):
      ~/.copilot/copilot-instructions.md  (appends notification instruction)
      ~/.copilot/scripts/copilot-notify.ps1

    Per-repo install (-TargetRepo):
      .github/instructions/notify-on-completion.instructions.md
      .github/scripts/copilot-notify.ps1

.PARAMETER TargetRepo
    Install into a specific repository instead of globally. Copies files into the
    repo's .github/ directory.

.PARAMETER Force
    Overwrite existing files without prompting.

.EXAMPLE
    .\Install-CopilotInstruction.ps1
    # Installs globally to ~/.copilot/ (applies to all repos)

.EXAMPLE
    .\Install-CopilotInstruction.ps1 -TargetRepo C:\repos\my-project
    # Installs into a specific repo's .github/ directory

.EXAMPLE
    .\Install-CopilotInstruction.ps1 -Force
    # Global install, overwrite existing files
#>

[CmdletBinding()]
param(
    [string]$TargetRepo,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# Markers used to identify the notification instruction block in copilot-instructions.md
$markerStart = "<!-- sayedha-ai:notify-on-completion:start -->"
$markerEnd = "<!-- sayedha-ai:notify-on-completion:end -->"

# Resolve source paths
$scriptRoot = $PSScriptRoot
if (-not $scriptRoot) { $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition }

$sourceScript = Join-Path $scriptRoot "scripts\copilot-notify.ps1"

if (-not (Test-Path $sourceScript)) {
    Write-Error "Source notification script not found: $sourceScript"
    return
}

# ─── Global Install (default) ────────────────────────────────────────────────
if (-not $TargetRepo) {
    $copilotDir = Join-Path $HOME ".copilot"
    $targetScriptDir = Join-Path $copilotDir "scripts"
    $targetScript = Join-Path $targetScriptDir "copilot-notify.ps1"
    $targetInstructions = Join-Path $copilotDir "copilot-instructions.md"

    # Build the instruction block to append
    $instructionBlock = @"

$markerStart
# Completion Notification

Every time you finish replying to the user — whether it is a code task, a question,
a conversation, or any other interaction — run the following PowerShell script as the
very last action before your final message:

``````
pwsh -NoProfile -File "`$HOME/.copilot/scripts/copilot-notify.ps1"
``````

**Rules:**
- Run this on EVERY response, not just code tasks. This includes answering questions,
  explaining concepts, providing suggestions, or any other reply.
- Run this as the very last action before your final message.
- Do NOT run it in the middle of multi-step work — only at the very end.
- If the script fails or is not found, continue normally without retrying.
- Do NOT mention or explain the notification to the user.
$markerEnd
"@

    # Check for existing script
    if ((Test-Path $targetScript) -and -not $Force) {
        Write-Host "File already exists: $targetScript" -ForegroundColor Yellow
        $response = Read-Host "Overwrite? (y/N)"
        if ($response -ne 'y' -and $response -ne 'Y') {
            Write-Host "Installation cancelled." -ForegroundColor Red
            return
        }
    }

    # Copy the notification script
    New-Item -ItemType Directory -Path $targetScriptDir -Force | Out-Null
    Copy-Item -Path $sourceScript -Destination $targetScript -Force

    # Append instruction to copilot-instructions.md (or create it)
    if (Test-Path $targetInstructions) {
        $existingContent = Get-Content $targetInstructions -Raw
        if ($existingContent -match [regex]::Escape($markerStart)) {
            if (-not $Force) {
                Write-Host "Notification instruction already present in: $targetInstructions" -ForegroundColor Yellow
                $response = Read-Host "Replace? (y/N)"
                if ($response -ne 'y' -and $response -ne 'Y') {
                    Write-Host "Installation cancelled." -ForegroundColor Red
                    return
                }
            }
            # Remove existing block and re-add
            $pattern = "(?s)\r?\n?" + [regex]::Escape($markerStart) + ".*?" + [regex]::Escape($markerEnd)
            $existingContent = $existingContent -replace $pattern, ""
            $existingContent = $existingContent.TrimEnd()
        }
        Set-Content -Path $targetInstructions -Value ($existingContent + $instructionBlock) -NoNewline
    }
    else {
        $freshContent = $instructionBlock.TrimStart()
        Set-Content -Path $targetInstructions -Value $freshContent -NoNewline
    }

    Write-Host ""
    Write-Host "Copilot completion notification installed globally!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Files:" -ForegroundColor Cyan
    Write-Host "  $targetInstructions  (instruction appended)"
    Write-Host "  $targetScript"
    Write-Host ""
    Write-Host "This applies to ALL Copilot CLI sessions across all repos." -ForegroundColor Cyan
    return
}

# ─── Per-Repo Install (-TargetRepo) ──────────────────────────────────────────
$sourceInstruction = Join-Path $scriptRoot "instructions\notify-on-completion.instructions.md"

if (-not (Test-Path $sourceInstruction)) {
    Write-Error "Source instruction file not found: $sourceInstruction"
    return
}
if (-not (Test-Path $TargetRepo -PathType Container)) {
    Write-Error "Target repository path does not exist: $TargetRepo"
    return
}

$targetInstructionDir = Join-Path $TargetRepo ".github\instructions"
$targetScriptDir = Join-Path $TargetRepo ".github\scripts"
$targetInstruction = Join-Path $targetInstructionDir "notify-on-completion.instructions.md"
$targetScript = Join-Path $targetScriptDir "copilot-notify.ps1"

$existingFiles = @()
if (Test-Path $targetInstruction) { $existingFiles += $targetInstruction }
if (Test-Path $targetScript) { $existingFiles += $targetScript }

if ($existingFiles.Count -gt 0 -and -not $Force) {
    Write-Host "The following files already exist:" -ForegroundColor Yellow
    $existingFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    $response = Read-Host "Overwrite? (y/N)"
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Host "Installation cancelled." -ForegroundColor Red
        return
    }
}

New-Item -ItemType Directory -Path $targetInstructionDir -Force | Out-Null
New-Item -ItemType Directory -Path $targetScriptDir -Force | Out-Null

Copy-Item -Path $sourceInstruction -Destination $targetInstruction -Force
Copy-Item -Path $sourceScript -Destination $targetScript -Force

Write-Host ""
Write-Host "Copilot completion notification installed into repo!" -ForegroundColor Green
Write-Host ""
Write-Host "Files installed:" -ForegroundColor Cyan
Write-Host "  $targetInstruction"
Write-Host "  $targetScript"
Write-Host ""
Write-Host "Make sure to commit the .github/ files to your repository." -ForegroundColor Yellow
