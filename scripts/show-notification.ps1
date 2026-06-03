param(
    [string]$Title,
    [string]$Message,
    [string]$Sound = 'Reminder',
    [switch]$Snooze,
    [string]$NotePath,
    [string]$TaskName
)

# Read title and message from note file if NotePath provided (avoids Chinese-in-command-line encoding issues)
if ($NotePath -and (Test-Path $NotePath)) {
    try {
        $noteContent = Get-Content $NotePath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if ($noteContent) {
            if (-not $Title -and $noteContent -match '(?m)^#\s+(.+)$') {
                $Title = $Matches[1].Trim()
            }
            if (-not $Message) {
                $lines = $noteContent -split "`n"
                $boldLines = $lines | Where-Object { $_ -match '^\*\*' }
                if ($boldLines.Count -ge 2 -and $boldLines[1] -match '\*\*.+?\*\*(.+)$') {
                    $Message = $Matches[1].Trim()
                }
            }
        }
    } catch { }
}

# Fallbacks
if (-not $Title)   { $Title = 'Reminder' }
if (-not $Message) { $Message = 'Time to check your reminder' }

# Ensure protocol handler is registered (one-time setup, self-healing)
& "$PSScriptRoot\register-protocol.ps1"

# Toast
if (-not (Get-Module -ListAvailable BurntToast)) {
    try { Install-PackageProvider -Name NuGet -Force -Scope CurrentUser; Install-Module -Name BurntToast -Force -Scope CurrentUser } catch { exit 1 }
}
Import-Module BurntToast -Force

$p = @{ Text = @($Title, $Message); Sound = $Sound }

# Build toast with BOTH native snooze dropdown AND Done button.
# BurntToast high-level API has mutually exclusive -Button/-SnoozeAndDismiss sets,
# so we build the toast content manually with New-BT* + Submit-BTNotification.
if ($NotePath) {
    $noteName = Split-Path $NotePath -Leaf
    $encoded  = [Uri]::EscapeDataString($noteName)

    $visual = New-BTVisual -BindingGeneric (New-BTBinding -Children @(
        New-BTText -Text $Title
        New-BTText -Text $Message
    ))

    $snoozeInput = New-BTInput -Id 'snoozeTime' -DefaultSelectionBoxItemId '5' -Items @(
        New-BTSelectionBoxItem -Id '1'  -Content '1 minute'
        New-BTSelectionBoxItem -Id '5'  -Content '5 minutes'
        New-BTSelectionBoxItem -Id '10' -Content '10 minutes'
        New-BTSelectionBoxItem -Id '30' -Content '30 minutes'
        New-BTSelectionBoxItem -Id '60' -Content '1 hour'
    )

    $action = New-BTAction -Inputs @($snoozeInput) -Buttons @(
        (New-BTButton -Snooze)
        (New-BTButton -Dismiss)
        (New-BTButton -Content 'Done' -Arguments "windows-reminder://done?note=$encoded" -ActivationType Protocol)
    )

    $content = New-BTContent -Visual $visual -Actions $action -Scenario Reminder
    Submit-BTNotification -Content $content
} elseif ($Snooze) {
    # Fallback: no NotePath, use simple snooze
    New-BurntToastNotification @p -SnoozeAndDismiss
} else {
    New-BurntToastNotification @p
}

# Update frontmatter: waiting -> reminded
if ($NotePath -and (Test-Path $NotePath)) {
    try {
        $content = Get-Content $NotePath -Raw -Encoding UTF8
        $updated = $content -replace '(?m)^status: waiting$', 'status: reminded'
        if ($updated -ne $content) { $updated | Set-Content $NotePath -Encoding UTF8 -NoNewline }
    } catch { }
}
