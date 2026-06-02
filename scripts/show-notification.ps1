param(
    [string]$Title,
    [string]$Message,
    [string]$Sound = 'Reminder',
    [switch]$Snooze,
    [string]$NotePath,
    [string]$TaskName
)

# ---- Toast ----
if (-not (Get-Module -ListAvailable BurntToast)) {
    try {
        Install-PackageProvider -Name NuGet -Force -Scope CurrentUser -ErrorAction Stop
        Install-Module -Name BurntToast -Force -Scope CurrentUser -ErrorAction Stop
    } catch { exit 1 }
}
Import-Module BurntToast -Force -ErrorAction Stop

$p = @{ Text = @($Title, $Message); Sound = $Sound }
if ($Snooze) { New-BurntToastNotification @p -SnoozeAndDismiss } else { New-BurntToastNotification @p }

# ---- Update note frontmatter: waiting -> reminded ----
if ($NotePath -and (Test-Path $NotePath)) {
    try {
        $content = Get-Content $NotePath -Raw -Encoding UTF8
        $updated = $content -replace '^status: waiting$', 'status: reminded'
        if ($updated -ne $content) {
            $updated | Set-Content $NotePath -Encoding UTF8 -NoNewline
        }
    } catch { }

    # Also update old table log for backward compat
    $logDir = Split-Path $NotePath -Parent
    $logFile = Get-ChildItem $logDir -Filter "提醒记录.md" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($logFile -and $TaskName) {
        try {
            $lines = Get-Content $logFile.FullName -Encoding UTF8
            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i].Contains($TaskName)) {
                    $parts = $lines[$i] -split '\|'
                    if ($parts.Count -ge 2) {
                        $bell = [char]0xD83D + [char]0xDD14
                        $parts[$parts.Count - 2] = " " + $bell + [char]0x5DF2 + [char]0x63D0 + [char]0x9192 + " "
                        $lines[$i] = $parts -join '|'
                    }
                    $lines | Set-Content $logFile.FullName -Encoding UTF8
                    break
                }
            }
        } catch { }
    }
}
