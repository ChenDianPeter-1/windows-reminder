# Runs at Windows logon. Finds missed reminders and fires them.
$vaultRoot = 'D:\AAAOddsAndEnds\PROGRAM\Obsidian Valut\Study'
$noteDir = "$vaultRoot\reminders"

if (-not (Test-Path $noteDir)) { exit 0 }

Get-ChildItem $noteDir -Filter "*.md" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
    if (-not $content) { return }

    # Check if it's a waiting reminder
    if ($content -notmatch '(?m)^status: waiting$') { return }
    if ($content -notmatch '(?m)^trigger:\s*(.+)$') { return }
    $triggerStr = $Matches[1].Trim()
    $trigger = try { Get-Date $triggerStr } catch { return }

    # Skip if not past due
    if ($trigger -gt (Get-Date)) { return }

    # Extract title and task name
    $taskName = if ($content -match '(?m)^task_name:\s*(.+)$') { $Matches[1].Trim() } else { '' }
    $message = if ($content -match '(?m)^#\s*(.+)$') { $Matches[1].Trim() } else { 'Missed reminder' }

    # Fire BurntToast notification
    if (Get-Module -ListAvailable BurntToast) {
        Import-Module BurntToast -Force
        New-BurntToastNotification -Text $message, "Missed reminder from $($trigger.ToString('MM-dd HH:mm'))" -Sound Reminder
    }

    # Update status
    $updated = $content -replace '(?m)^status: waiting$', 'status: reminded'
    $updated | Set-Content $_.FullName -Encoding UTF8 -NoNewline
}
