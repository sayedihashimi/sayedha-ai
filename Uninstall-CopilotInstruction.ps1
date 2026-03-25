<#
.SYNOPSIS
    Removes the Copilot completion notification from a target repository.

.DESCRIPTION
    Removes the instruction file and notification script that were installed by
    Install-CopilotInstruction.ps1. Cleans up empty directories afterward.

.PARAMETER TargetRepo
    Path to the target repository. Defaults to the current directory.

.EXAMPLE
    .\Uninstall-CopilotInstruction.ps1
    # Removes from the current directory

.EXAMPLE
    .\Uninstall-CopilotInstruction.ps1 -TargetRepo C:\repos\my-project
#>

[CmdletBinding()]
param(
    [string]$TargetRepo = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

# Define target paths
$targetInstruction = Join-Path $TargetRepo ".github\instructions\notify-on-completion.instructions.md"
$targetScript = Join-Path $TargetRepo ".github\scripts\copilot-notify.ps1"

$removed = @()

# Remove instruction file
if (Test-Path $targetInstruction) {
    Remove-Item $targetInstruction -Force
    $removed += $targetInstruction
    Write-Host "Removed: $targetInstruction" -ForegroundColor Green

    # Clean up empty instructions directory
    $instructionDir = Split-Path $targetInstruction -Parent
    if ((Test-Path $instructionDir) -and (Get-ChildItem $instructionDir | Measure-Object).Count -eq 0) {
        Remove-Item $instructionDir -Force
        Write-Host "Removed empty directory: $instructionDir" -ForegroundColor DarkGray
    }
}

# Remove notification script
if (Test-Path $targetScript) {
    Remove-Item $targetScript -Force
    $removed += $targetScript
    Write-Host "Removed: $targetScript" -ForegroundColor Green

    # Clean up empty scripts directory
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
    Write-Host "Copilot completion notification uninstalled successfully." -ForegroundColor Green
    Write-Host "Don't forget to commit the changes." -ForegroundColor Yellow
}
