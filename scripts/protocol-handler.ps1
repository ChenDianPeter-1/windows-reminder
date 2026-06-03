param(
    [string]$Url
)

# Parse: windows-reminder://done?note=YYYY-MM-DD-slug.md
if (-not $Url) { exit 1 }
if ($Url -notmatch 'windows-reminder://done\?note=(.+)$') { exit 1 }

$noteName = [Uri]::UnescapeDataString($Matches[1])
$vaultRoot = 'D:\AAAOddsAndEnds\PROGRAM\Obsidian Valut\Study'

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
