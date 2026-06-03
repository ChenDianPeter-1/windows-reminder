# Windows Reminder Daemon
# Single long-running process that polls reminders/ every 60s.
# Replaces per-reminder Start-Sleep background processes.
# Launched by startup-check.ps1 on boot; mutex prevents duplicates.

$vaultRoot = 'D:\AAAOddsAndEnds\PROGRAM\Obsidian Valut\Study'
$noteDir   = Join-Path $vaultRoot 'reminders'
$showNotif = Join-Path $PSScriptRoot 'show-notification.ps1'

# Prevent duplicate instances via named mutex
$mutex = New-Object System.Threading.Mutex($false, 'Global\WindowsReminderDaemon')
if (-not $mutex.WaitOne(0)) { exit 0 }

# Kill any lingering per-reminder timer processes (old architecture)
Get-WmiObject Win32_Process -Filter "Name='powershell.exe'" | ForEach-Object {
    $cmd = $_.CommandLine
    if ($cmd -and $cmd -match 'Start-Sleep.*show-notification') {
        try { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue } catch { }
    }
}

while ($true) {
    if (-not (Test-Path $noteDir)) { Start-Sleep -Seconds 60; continue }

    $now = Get-Date
    Get-ChildItem $noteDir -Filter '*.md' | ForEach-Object {
        $content = Get-Content $_.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if (-not $content) { return }
        if ($content -notmatch '(?m)^trigger:\s*(.+)$') { return }

        $trigger = try { Get-Date $Matches[1].Trim() } catch { return }
        if ($trigger -gt $now) { return }

        # Only fire if status is still waiting
        $statusLine = if ($content -match '(?m)^status:\s*(\S+)$') { $Matches[1] } else { '' }
        if ($statusLine -ne 'waiting') { return }

        $taskName = if ($content -match '(?m)^task_name:\s*(.+)$') { $Matches[1].Trim() } else { '' }
        & $showNotif -Sound Reminder -NotePath $_.FullName -TaskName $taskName
    }

    Start-Sleep -Seconds 60
}
