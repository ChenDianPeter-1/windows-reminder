param(
    [string]$Title,
    [string]$Message,
    [string]$Sound = 'Reminder',
    [switch]$Snooze,
    [string]$NotePath,
    [string]$TaskName
)

# Toast
if (-not (Get-Module -ListAvailable BurntToast)) {
    try { Install-PackageProvider -Name NuGet -Force -Scope CurrentUser; Install-Module -Name BurntToast -Force -Scope CurrentUser } catch { exit 1 }
}
Import-Module BurntToast -Force
$p = @{ Text = @($Title, $Message); Sound = $Sound }
if ($Snooze) { New-BurntToastNotification @p -SnoozeAndDismiss } else { New-BurntToastNotification @p }

# Update frontmatter: waiting -> reminded
if ($NotePath -and (Test-Path $NotePath)) {
    try {
        $content = Get-Content $NotePath -Raw -Encoding UTF8
        $updated = $content -replace '(?m)^status: waiting$', 'status: reminded'
        if ($updated -ne $content) { $updated | Set-Content $NotePath -Encoding UTF8 -NoNewline }
    } catch { }
}
