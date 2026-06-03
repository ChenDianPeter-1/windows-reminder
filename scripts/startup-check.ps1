<#
.SYNOPSIS
    Runs at Windows logon. Fires missed reminders whose trigger time has passed.
    Registered via registry Run key, NOT a separate VBS launcher.
#>

$vaultRoot = 'D:\AAAOddsAndEnds\PROGRAM\Obsidian Valut\Study'
$noteDir = Join-Path $vaultRoot 'reminders'
if (-not (Test-Path $noteDir)) { exit 0 }

$now = Get-Date
$fired = 0

Get-ChildItem $noteDir -Filter '*.md' | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
    if (-not $content) { return }
    if ($content -notmatch '(?m)^status: waiting$') { return }
    if ($content -notmatch '(?m)^trigger:\s*(.+)$') { return }

    $trigger = try { Get-Date $Matches[1].Trim() } catch { return }
    if ($trigger -gt $now) { return }

    $message = if ($content -match '(?m)^#\s*(.+)$') { $Matches[1].Trim() } else { 'Missed reminder' }
    $taskName = if ($content -match '(?m)^task_name:\s*(.+)$') { $Matches[1].Trim() } else { '' }

    # Fire notification
    if (Get-Module -ListAvailable BurntToast) {
        Import-Module BurntToast -Force -ErrorAction SilentlyContinue
        $noteName = Split-Path $_.FullName -Leaf
        $button = New-BTButton -Content '完成' -Arguments "windows-reminder://done?note=$([Uri]::EscapeDataString($noteName))" -ActivationType Protocol
        New-BurntToastNotification -Text $message, "Missed: $($trigger.ToString('MM-dd HH:mm'))" -Sound Reminder -Button $button -ErrorAction SilentlyContinue
    }

    # Update status
    $updated = $content -replace '(?m)^status: waiting$', 'status: reminded'
    $updated | Set-Content $_.FullName -Encoding UTF8 -NoNewline -ErrorAction SilentlyContinue
    $fired++
}

# Register self to Run key if not already (self-healing)
$regPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$regName = 'WindowsReminder'
$regValue = "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$PSCommandPath`""
$current = Get-ItemProperty -Path $regPath -Name $regName -ErrorAction SilentlyContinue
if (-not $current -or $current.$regName -ne $regValue) {
    Set-ItemProperty -Path $regPath -Name $regName -Value $regValue -Force
}

# Register custom protocol handler (self-healing)
& "$PSScriptRoot\register-protocol.ps1"
