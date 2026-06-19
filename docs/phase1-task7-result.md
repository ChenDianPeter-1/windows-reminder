# Task 7: Release Hardening — Stable Runtime Path

## Date
2026-06-19

## Status: ✅ COMPLETE

## Published Release

| Item | Value |
|------|-------|
| Command | `dotnet publish -c Release -o %LOCALAPPDATA%\WindowsReminder` |
| Stable EXE | `C:\Users\chenjunjin\AppData\Local\WindowsReminder\WindowsReminder.exe` |
| Build | Release, self-contained (framework-dependent) |

## Path Updates

| Item | Before (Task 6) | After (Task 7) |
|------|-----------------|----------------|
| Run key | `...\src\WindowsReminder\bin\Debug\...\WindowsReminder.exe` | `%LOCALAPPDATA%\WindowsReminder\WindowsReminder.exe` |
| Start Menu shortcut | `...\src\WindowsReminder\bin\Debug\...\WindowsReminder.exe` | `%LOCALAPPDATA%\WindowsReminder\WindowsReminder.exe` |

## Verification from Release EXE

| Check | Result |
|-------|--------|
| `dotnet publish -c Release` succeeds | ✅ |
| Stable EXE exists | ✅ |
| Run key points to stable dir (not bin/Debug) | ✅ |
| Start Menu shortcut points to stable dir | ✅ |
| Launch from stable dir works | ✅ (PID 24240) |
| Tray icon appears | ✅ |
| Scanner runs | ✅ |
| AutoStart enabled | ✅ |
| Test reminder: waiting→toast→reminded→Done→done | ✅ |
| Log output normal | ✅ |

## Log Confirmation
```
16:25:18 App starting — reminders path: D:\...\Study\reminders
16:25:19 Scanner started. Path=... Interval=30s DryRun=false
16:25:19 AutoStart enabled: "C:\Users\...\Local\WindowsReminder\WindowsReminder.exe"
16:25:19 Tray icon initialized
```

## Recommendation

Desktop shortcut is functional. Daily use ready.
