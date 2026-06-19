# Task 1: Project Skeleton + Tray + Single Instance + Toast Infrastructure

## Date
2026-06-18

## Status: ✅ PASSED

All acceptance criteria met.

## Environment

| Item | Value |
|------|-------|
| .NET SDK | 8.0.422 |
| Target | net8.0-windows10.0.19041.0 |
| WPF | Yes |
| Windows Forms (for NotifyIcon) | Yes |
| Toast Library | Microsoft.Toolkit.Uwp.Notifications 7.1.3 |
| DI / Hosting | Microsoft.Extensions.Hosting 8.0.1 |
| Logging | Serilog 4.0.2 + Serilog.Sinks.File 6.0.0 |

## Acceptance Criteria Results

| # | Criterion | Result |
|---|-----------|--------|
| 1 | `dotnet build` succeeds | ✅ |
| 2 | `dotnet run` shows tray icon | ✅ (in overflow area on Win11) |
| 3 | Right-click context menu appears | ✅ |
| 4 | About shows info dialog | ✅ |
| 5 | Test Toast fires notification with Done button | ✅ |
| 6 | Clicking Done logs "done callback received" | ✅ (verified in logs) |
| 7 | Second instance does not create second tray icon | ✅ (Mutex blocks, logs "second instance detected") |
| 8 | Exit terminates process cleanly | ✅ |
| 9 | Start Menu shortcut exists | ✅ `%APPDATA%\Microsoft\Windows\Start Menu\Programs\WindowsReminder.lnk` |

## Project Structure Created

```
src/WindowsReminder/
├── WindowsReminder.csproj          # WPF + WinForms, net8.0-windows
├── App.xaml                        # WPF Application (OnExplicitShutdown)
├── App.xaml.cs                     # Startup: config, Serilog, DI, single instance, tray
├── appsettings.json                # AppName, AUMID, paths, poll interval
├── Models/
│   └── AppSettings.cs              # Configuration POCO
├── Services/
│   ├── SingleInstanceService.cs    # Mutex-based single-instance guard
│   ├── ToastNotificationService.cs # Toast wrapper (Toolkit), OnActivated handler
│   └── StartMenuShortcutService.cs # Creates Start Menu lnk for toast identity
├── Tray/
│   └── TrayIconManager.cs          # NotifyIcon + context menu (About/TestToast/Log/Exit)
└── Logging/                        # (empty, Serilog configured in App.xaml.cs)
```

## Key Design Decisions

### Why System.Windows.Forms.NotifyIcon (not H.NotifyIcon.Wpf)
H.NotifyIcon.Wpf 2.4.1 only targets .NET Framework 4.x. It would work via compatibility shim (NU1701 warning) but the built-in WinForms NotifyIcon is battle-tested and requires no NuGet dependency. Added `<UseWindowsForms>true</UseWindowsForms>` to csproj.

### Why Microsoft.Toolkit.Uwp.Notifications (not Windows App SDK)
The Windows App SDK's `AppNotificationManager` requires Visual Studio UWP build components (`Microsoft.Build.Packaging.Pri.Tasks.dll`) that are not available in a `dotnet` CLI-only environment. The Toolkit is deprecated but fully functional and was verified in Task 0 POC. Migration to App SDK can happen later when Visual Studio is installed.

### Why Start Menu Shortcut is Required
Unpackaged desktop apps using toast notifications MUST have a Start Menu shortcut. Without it, Windows cannot resolve the app identity for toast activation callbacks (the `OnActivated` event won't fire). This is NOT optional — it's a Windows platform requirement.

## Known Issues

1. **Tray icon in overflow**: Win11 hides new tray icons by default. Users must drag it to the taskbar or configure via Settings → Personalization → Taskbar → Other system tray icons.

2. **Generic icon**: Currently using `System.Drawing.SystemIcons.Application`. Will be replaced with a custom icon in Task 2.

3. **No app icon for toast**: Toast notifications don't show app icon yet. Requires either an MSIX package or the Start Menu shortcut to have an icon set (future task).

## Log Verification

```
2026-06-18 17:35:35 [INF] Single instance acquired: Global\ChenDianPeter.WindowsReminder.SingleInstance
2026-06-18 17:35:36 [INF] App starting
2026-06-18 17:35:36 [INF] Start Menu shortcut created: C:\Users\...\Start Menu\Programs\WindowsReminder.lnk
2026-06-18 17:35:36 [INF] Tray icon initialized
```

## Recommendation

**Proceed to Task 2** (reminder scanning + Markdown parsing + toast with real content).
