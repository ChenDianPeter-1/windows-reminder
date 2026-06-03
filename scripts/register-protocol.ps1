# Register windows-reminder:// custom protocol handler
# Run once (or on every startup-check for self-healing)

$handlerPath = Join-Path $PSScriptRoot 'protocol-handler.ps1'
$psCmd = "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$handlerPath`" `"%1`""

$regRoot = 'HKCU:\Software\Classes\windows-reminder'

# Check if already registered correctly
$current = Get-ItemProperty -Path "$regRoot\shell\open\command" -Name '(default)' -ErrorAction SilentlyContinue
if ($current -and $current.'(default)' -eq $psCmd) { exit 0 }

# Create/update registry entries
New-Item -Path $regRoot -Force -ErrorAction SilentlyContinue | Out-Null
Set-ItemProperty -Path $regRoot -Name '(default)' -Value 'URL:Windows Reminder Protocol' -Force
Set-ItemProperty -Path $regRoot -Name 'URL Protocol' -Value '' -Force
New-Item -Path "$regRoot\shell\open\command" -Force -ErrorAction SilentlyContinue | Out-Null
Set-ItemProperty -Path "$regRoot\shell\open\command" -Name '(default)' -Value $psCmd -Force

Write-Host "Protocol windows-reminder:// registered successfully"
