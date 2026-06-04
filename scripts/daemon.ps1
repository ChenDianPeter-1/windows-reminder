# Windows Reminder Daemon
# Single long-running process that polls reminders/ every 60s.
# Launched by startup-check.ps1 on boot; mutex prevents duplicates.

$vaultRoot = 'D:\AAAOddsAndEnds\PROGRAM\Obsidian Valut\Study'
$noteDir   = Join-Path $vaultRoot 'reminders'
$showNotif = Join-Path $PSScriptRoot 'show-notification.ps1'
$logFile   = Join-Path $env:TEMP 'windows-reminder-daemon.log'

function Write-Log($msg) {
    $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    "$ts $msg" | Out-File $logFile -Append -Encoding utf8
}

# Prevent duplicate instances via named mutex
$createdNew = $false
$mutex = New-Object System.Threading.Mutex($true, 'Global\WindowsReminderDaemon', [ref]$createdNew)
if (-not $createdNew) {
    Write-Log 'Another daemon already running, exiting'
    exit 0
}
Write-Log 'Daemon started'

# Kill any lingering per-reminder Start-Sleep processes (old architecture)
$killed = 0
Get-WmiObject Win32_Process -Filter "Name='powershell.exe'" | ForEach-Object {
    $cmd = $_.CommandLine
    if ($cmd -and $cmd -match 'Start-Sleep.*show-notification') {
        try { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue; $killed++ } catch { }
    }
}
if ($killed -gt 0) { Write-Log "Killed $killed legacy timer process(es)" }

while ($true) {
    if (-not (Test-Path $noteDir)) { Start-Sleep -Seconds 60; continue }

    $now = Get-Date
    $fired = 0
    Get-ChildItem $noteDir -Filter '*.md' | ForEach-Object {
        $content = Get-Content $_.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if (-not $content) { return }
        if ($content -notmatch '(?m)^trigger:\s*(.+)$') { return }

        $trigger = try { Get-Date $Matches[1].Trim() } catch { return }
        if ($trigger -gt $now) { return }

        $statusLine = if ($content -match '(?m)^status:\s*(\S+)$') { $Matches[1] } else { '' }
        if ($statusLine -ne 'waiting') { return }

        $taskName = if ($content -match '(?m)^task_name:\s*(.+)$') { $Matches[1].Trim() } else { '' }
        $title    = if ($content -match '(?m)^#\s+(.+)$') { $Matches[1].Trim() } else { 'Reminder' }
        Write-Log "Firing: [$title] trigger=$($trigger.ToString('HH:mm')) note=$($_.Name)"

        try {
            & $showNotif -Sound Reminder -NotePath $_.FullName -TaskName $taskName
            $fired++
        } catch {
            Write-Log "ERROR firing notification: $_"
        }
    }
    if ($fired -gt 0) { Write-Log "Fired $fired notification(s)" }

    Start-Sleep -Seconds 60
}

# Keep mutex alive (never reached, but GC safety)
[GC]::KeepAlive($mutex)
