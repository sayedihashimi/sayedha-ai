<#
.SYNOPSIS
    Removes the Copilot completion notification (global or per-repo).

.DESCRIPTION
    Removes the notification instruction and script installed by Install-CopilotInstruction.ps1.
    By default, removes the global install from ~/.copilot/. Use -TargetRepo to remove
    from a specific repository instead.

.PARAMETER TargetRepo
    Remove from a specific repository instead of the global install.

.EXAMPLE
    .\Uninstall-CopilotInstruction.ps1
    # Removes global install from ~/.copilot/

.EXAMPLE
    .\Uninstall-CopilotInstruction.ps1 -TargetRepo C:\repos\my-project
    # Removes from a specific repo
#>

[CmdletBinding()]
param(
    [string]$TargetRepo
)

$ErrorActionPreference = 'Stop'

$markerStart = "<!-- sayedha-ai:notify-on-completion:start -->"
$markerEnd = "<!-- sayedha-ai:notify-on-completion:end -->"

# ─── Global Uninstall (default) ──────────────────────────────────────────────
if (-not $TargetRepo) {
    $copilotDir = Join-Path $HOME ".copilot"
    $targetScript = Join-Path $copilotDir "scripts\copilot-notify.ps1"
    $targetInstructions = Join-Path $copilotDir "copilot-instructions.md"
    $removed = @()

    # Remove the notification script
    if (Test-Path $targetScript) {
        Remove-Item $targetScript -Force
        $removed += $targetScript
        Write-Host "Removed: $targetScript" -ForegroundColor Green

        $scriptDir = Split-Path $targetScript -Parent
        if ((Test-Path $scriptDir) -and (Get-ChildItem $scriptDir | Measure-Object).Count -eq 0) {
            Remove-Item $scriptDir -Force
            Write-Host "Removed empty directory: $scriptDir" -ForegroundColor DarkGray
        }
    }

    # Remove the instruction block from copilot-instructions.md
    if (Test-Path $targetInstructions) {
        $content = Get-Content $targetInstructions -Raw
        if ($content -match [regex]::Escape($markerStart)) {
            $pattern = "(?s)\r?\n?" + [regex]::Escape($markerStart) + ".*?" + [regex]::Escape($markerEnd)
            $content = $content -replace $pattern, ""
            $content = $content.TrimEnd()

            if ([string]::IsNullOrWhiteSpace($content)) {
                Remove-Item $targetInstructions -Force
                Write-Host "Removed: $targetInstructions (was empty after removal)" -ForegroundColor Green
            }
            else {
                Set-Content -Path $targetInstructions -Value $content -NoNewline
                Write-Host "Removed notification block from: $targetInstructions" -ForegroundColor Green
            }
            $removed += $targetInstructions
        }
    }

    if ($removed.Count -eq 0) {
        Write-Host "No Copilot notification files found in: $copilotDir" -ForegroundColor Yellow
    }
    else {
        Write-Host ""
        Write-Host "Copilot completion notification uninstalled globally." -ForegroundColor Green
    }
    return
}

# ─── Per-Repo Uninstall (-TargetRepo) ────────────────────────────────────────
$targetInstruction = Join-Path $TargetRepo ".github\instructions\notify-on-completion.instructions.md"
$targetScript = Join-Path $TargetRepo ".github\scripts\copilot-notify.ps1"
$removed = @()

if (Test-Path $targetInstruction) {
    Remove-Item $targetInstruction -Force
    $removed += $targetInstruction
    Write-Host "Removed: $targetInstruction" -ForegroundColor Green

    $instructionDir = Split-Path $targetInstruction -Parent
    if ((Test-Path $instructionDir) -and (Get-ChildItem $instructionDir | Measure-Object).Count -eq 0) {
        Remove-Item $instructionDir -Force
        Write-Host "Removed empty directory: $instructionDir" -ForegroundColor DarkGray
    }
}

if (Test-Path $targetScript) {
    Remove-Item $targetScript -Force
    $removed += $targetScript
    Write-Host "Removed: $targetScript" -ForegroundColor Green

    $scriptDir = Split-Path $targetScript -Parent
    if ((Test-Path $scriptDir) -and (Get-ChildItem $scriptDir | Measure-Object).Count -eq 0) {
        Remove-Item $scriptDir -Force
        Write-Host "Removed empty directory: $scriptDir" -ForegroundColor DarkGray
    }
}

if ($removed.Count -eq 0) {
    Write-Host "No Copilot notification files found in: $TargetRepo" -ForegroundColor Yellow
}
else {
    Write-Host ""
    Write-Host "Copilot completion notification uninstalled from repo." -ForegroundColor Green
    Write-Host "Don't forget to commit the changes." -ForegroundColor Yellow
}
