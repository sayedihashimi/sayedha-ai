<#
.SYNOPSIS
    Installs the Copilot completion notification into a target repository.

.DESCRIPTION
    Copies the Copilot instruction file and notification script into the target
    repository's .github/ directory so that Copilot will automatically play a
    sound and show a notification when it completes a task.

    Files installed:
      .github/instructions/notify-on-completion.instructions.md
      .github/scripts/copilot-notify.ps1

.PARAMETER TargetRepo
    Path to the target repository. Defaults to the current directory.

.PARAMETER Force
    Overwrite existing files without prompting.

.EXAMPLE
    .\Install-CopilotInstruction.ps1
    # Installs into the current directory

.EXAMPLE
    .\Install-CopilotInstruction.ps1 -TargetRepo C:\repos\my-project
    # Installs into a specific repo

.EXAMPLE
    .\Install-CopilotInstruction.ps1 -TargetRepo C:\repos\my-project -Force
    # Overwrites existing files
#>

[CmdletBinding()]
param(
    [string]$TargetRepo = (Get-Location).Path,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# Resolve paths
$scriptRoot = $PSScriptRoot
if (-not $scriptRoot) { $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition }

$sourceInstruction = Join-Path $scriptRoot "instructions\notify-on-completion.instructions.md"
$sourceScript = Join-Path $scriptRoot "scripts\copilot-notify.ps1"

# Validate source files exist
if (-not (Test-Path $sourceInstruction)) {
    Write-Error "Source instruction file not found: $sourceInstruction"
    return
}
if (-not (Test-Path $sourceScript)) {
    Write-Error "Source notification script not found: $sourceScript"
    return
}

# Validate target is a directory
if (-not (Test-Path $TargetRepo -PathType Container)) {
    Write-Error "Target repository path does not exist: $TargetRepo"
    return
}

# Define target paths
$targetInstructionDir = Join-Path $TargetRepo ".github\instructions"
$targetScriptDir = Join-Path $TargetRepo ".github\scripts"
$targetInstruction = Join-Path $targetInstructionDir "notify-on-completion.instructions.md"
$targetScript = Join-Path $targetScriptDir "copilot-notify.ps1"

# Check for existing files
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

# Create target directories
New-Item -ItemType Directory -Path $targetInstructionDir -Force | Out-Null
New-Item -ItemType Directory -Path $targetScriptDir -Force | Out-Null

# Copy files
Copy-Item -Path $sourceInstruction -Destination $targetInstruction -Force
Copy-Item -Path $sourceScript -Destination $targetScript -Force

Write-Host ""
Write-Host "Copilot completion notification installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Files installed:" -ForegroundColor Cyan
Write-Host "  $targetInstruction"
Write-Host "  $targetScript"
Write-Host ""
Write-Host "Copilot will now play a sound and show a notification when it completes a task." -ForegroundColor Cyan
Write-Host "Make sure to commit the .github/ files to your repository." -ForegroundColor Yellow
