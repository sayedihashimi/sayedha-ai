<#
.SYNOPSIS
    Plays a completion sound and shows a Windows notification when Copilot finishes a task.

.DESCRIPTION
    This script is designed to be invoked by GitHub Copilot (via a custom instruction) at the
    end of every task. It plays a pleasant multi-tone melody and displays a Windows balloon
    notification in the system tray.

    Requires PowerShell 7+ (PowerShell Core) on Windows. Uses only built-in .NET APIs —
    no external modules required.

.PARAMETER Message
    Optional custom message for the notification body. Defaults to "Task completed!".

.PARAMETER Title
    Optional custom title for the notification. Defaults to "GitHub Copilot".

.PARAMETER NoSound
    Skip the completion melody and only show the notification.

.PARAMETER NoNotification
    Skip the notification and only play the completion melody.

.EXAMPLE
    .\copilot-notify.ps1
    # Plays melody + shows default notification

.EXAMPLE
    .\copilot-notify.ps1 -Message "Build succeeded!" -Title "Copilot Build"
    # Custom message and title

.EXAMPLE
    .\copilot-notify.ps1 -NoNotification
    # Sound only, no popup
#>

[CmdletBinding()]
param(
    [string]$Message = "Task completed!",
    [string]$Title = "GitHub Copilot",
    [switch]$NoSound,
    [switch]$NoNotification
)

# --- Play completion melody ---
if (-not $NoSound) {
    try {
        # Look for the WAV file next to this script
        $wavPath = Join-Path (Split-Path -Parent $PSCommandPath) "copilot-notify.wav"
        if (Test-Path $wavPath) {
            # Play the WAV file using .NET SoundPlayer (rich Dm-style jingle)
            Add-Type -AssemblyName System.IO -ErrorAction SilentlyContinue
            $player = New-Object System.Media.SoundPlayer $wavPath
            $player.PlaySync()
            $player.Dispose()
        }
        else {
            # Fallback to system beeps if WAV not found
            [System.Console]::Beep(523, 150)   # C5
            [System.Console]::Beep(587, 150)   # D5
            [System.Console]::Beep(659, 150)   # E5
            [System.Console]::Beep(784, 300)   # G5
        }
    }
    catch {
        Write-Verbose "Could not play completion sound: $_"
    }
}

# --- Show Windows balloon notification ---
if (-not $NoNotification) {
    try {
        Add-Type -AssemblyName System.Windows.Forms -ErrorAction Stop
        Add-Type -AssemblyName System.Drawing -ErrorAction Stop

        $notify = New-Object System.Windows.Forms.NotifyIcon
        $notify.Icon = [System.Drawing.SystemIcons]::Information
        $notify.BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Info
        $notify.BalloonTipTitle = $Title
        $notify.BalloonTipText = $Message
        $notify.Visible = $true
        $notify.ShowBalloonTip(5000)

        # Keep the icon alive briefly so the notification renders, then clean up
        Start-Sleep -Seconds 4
        $notify.Dispose()
    }
    catch {
        Write-Verbose "Could not show notification: $_"
    }
}
