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

# "Done" button via custom protocol (default); snooze is opt-in since -Button and -SnoozeAndDismiss are mutually exclusive
if ($NotePath) {
    $noteName = Split-Path $NotePath -Leaf
    $button = New-BTButton -Content '完成' -Arguments "windows-reminder://done?note=$([Uri]::EscapeDataString($noteName))" -ActivationType Protocol
    $p['Button'] = $button
}
if ($Snooze) { $p.Remove('Button'); $p['SnoozeAndDismiss'] = $true }

New-BurntToastNotification @p

# Update frontmatter: waiting -> reminded
if ($NotePath -and (Test-Path $NotePath)) {
    try {
        $content = Get-Content $NotePath -Raw -Encoding UTF8
        $updated = $content -replace '(?m)^status: waiting$', 'status: reminded'
        if ($updated -ne $content) { $updated | Set-Content $NotePath -Encoding UTF8 -NoNewline }
    } catch { }
}
