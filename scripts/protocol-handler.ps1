param(
    [string]$Url
)

if (-not $Url) { exit 1 }

$vaultRoot = 'D:\AAAOddsAndEnds\PROGRAM\Obsidian Valut\Study'
$script   = 'C:\Users\chenjunjin\.claude\skills\windows-reminder\scripts\show-notification.ps1'

# --- Action: done ---  windows-reminder://done?note=YYYY-MM-DD-slug.md
if ($Url -match 'windows-reminder://done\?note=(.+)$') {
    $noteName = [Uri]::UnescapeDataString($Matches[1])
    $notePath = Get-ChildItem $vaultRoot -Recurse -Filter $noteName -ErrorAction SilentlyContinue |
        Select-Object -First 1 | ForEach-Object { $_.FullName }
    if (-not $notePath) { exit 1 }
    try {
        $content = Get-Content $notePath -Raw -Encoding UTF8
        $updated = $content -replace '(?m)^status: (waiting|reminded)$', 'status: done'
        if ($updated -ne $content) {
            $updated | Set-Content $notePath -Encoding UTF8 -NoNewline
        }
    } catch { }
    exit 0
}

# --- Action: snooze ---  windows-reminder://snooze?note=YYYY-MM-DD-slug.md
if ($Url -match 'windows-reminder://snooze\?note=(.+)$') {
    $noteName = [Uri]::UnescapeDataString($Matches[1])
    $notePath = Get-ChildItem $vaultRoot -Recurse -Filter $noteName -ErrorAction SilentlyContinue |
        Select-Object -First 1 | ForEach-Object { $_.FullName }
    if (-not $notePath) { exit 1 }

    # Reset status to waiting so it fires again
    try {
        $content = Get-Content $notePath -Raw -Encoding UTF8
        $updated = $content -replace '(?m)^status: reminded$', 'status: waiting'
        if ($updated -ne $content) {
            $updated | Set-Content $notePath -Encoding UTF8 -NoNewline
        }
    } catch { }

    # Pick a new task name to avoid collision
    $taskName = "ClaudeReminder-snooze-$((Get-Date).ToString('HHmmss'))"

    # Start a 5-minute background timer (pure ASCII — no Chinese)
    $bgCmd = "Start-Sleep -Seconds 300; & '$script' -Sound Reminder -NotePath '$notePath' -TaskName '$taskName'"
    Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-WindowStyle', 'Hidden', '-Command', $bgCmd) -WindowStyle Hidden
    exit 0
}
