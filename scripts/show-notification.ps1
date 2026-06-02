<#
.SYNOPSIS
    Display a native Windows toast notification via BurntToast.
    Called by Windows Task Scheduler for timed reminders.
.PARAMETER Title
    Notification title (bold, first line)
.PARAMETER Message
    Notification body text
.PARAMETER Sound
    Windows sound alias: Default, IM, Mail, Reminder, SMS, Alarm, Alarm2-10, Call, Call2-10
.PARAMETER Snooze
    If set, adds Snooze & Dismiss buttons to the toast
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Title,
    [Parameter(Mandatory=$true)]
    [string]$Message,
    [string]$Sound = 'Reminder',
    [switch]$Snooze
)

# Ensure BurntToast module is available
if (-not (Get-Module -ListAvailable BurntToast)) {
    try {
        Install-PackageProvider -Name NuGet -Force -Scope CurrentUser -ErrorAction Stop
        Install-Module -Name BurntToast -Force -Scope CurrentUser -ErrorAction Stop
    } catch {
        Write-Error "Failed to install BurntToast: $_"
        exit 1
    }
}

Import-Module BurntToast -Force -ErrorAction Stop

$params = @{
    Text  = @($Title, $Message)
    Sound = $Sound
}

if ($Snooze) {
    New-BurntToastNotification @params -SnoozeAndDismiss
} else {
    New-BurntToastNotification @params
}
