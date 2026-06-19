# Task 0: Toast POC Verification Report

## Environment

| Item | Value |
|------|-------|
| .NET SDK | 8.0.422 (installed via dotnet-install.ps1) |
| Target Framework | net8.0-windows10.0.19041.0 |
| UI Framework | WPF |
| Toast Library | Microsoft.Toolkit.Uwp.Notifications 7.1.3 |
| OS | Windows 10/11 |
| AppIdentity | Unpackaged (no MSIX) |

## What was tested

A minimal WPF app (`WindowsReminder.Poc`) that:
1. Subscribes to `ToastNotificationManagerCompat.OnActivated`
2. Sends a toast notification with a **Done** button (`action=done`)
3. Awaits the `OnActivated` callback when the button is clicked

## Results

| Check | Result |
|-------|--------|
| `dotnet build` succeeds | ✅ |
| Toast appears | ✅ (user confirmed visually in earlier run) |
| `OnActivated` fires on button click | ✅ |
| `args.Argument` contains `"action=done"` | ✅ |
| No COM registration exception | ✅ |
| No AUMID exception | ✅ |
| Process exits cleanly | ✅ |

### Console output from successful run

```
[POC] Toast sent — look for it and click 'Done'
[POC] Activated — args: 'action=done'
[POC] PASS: Done callback received!
```

## Critical Dependency: Start Menu Shortcut

Unpackaged apps using `Microsoft.Toolkit.Uwp.Notifications` **must** have a shortcut in:
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\
```

Without this shortcut, `OnActivated` will not fire. The POC shortcut was created at:
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\WindowsReminder.Poc.lnk
```

This is the unpackaged equivalent of MSIX package identity. The toast activation system uses the shortcut to resolve which EXE to launch for the callback.

## Known Limitations of Current Approach

1. **`Microsoft.Toolkit.Uwp.Notifications` is archived/deprecated** — Microsoft recommends migrating to **Windows App SDK `AppNotificationManager`**. However, AppNotificationManager requires Visual Studio build components (specifically `Microsoft.Build.Packaging.Pri.Tasks.dll` from the UWP workload), which is not available in a `dotnet` CLI-only environment. The Toolkit approach is the pragmatic choice for now.

2. **Shortcut must exist before first toast** — The production app must create this shortcut on install/first-run.

3. **Foreground activation only** — `ToastActivationType.Foreground` brings the app window to front when Done is clicked. For a tray-only app, this would need adjustment (the window should not steal focus).

## Recommendation

**Proceed to Task 1** (Phase 1 MVP skeleton). The highest-risk technology — toast + Done callback in an unpackaged WPF app — is validated.

### What to carry forward

- `Microsoft.Toolkit.Uwp.Notifications` as the toast library
- Start Menu shortcut registration as an install/first-run step
- `OnActivated` event for Done button callbacks
- `.NET 8.0` + WPF as the base technology

### What to change for production

- When upgrading to Windows App SDK (requires Visual Studio installation), migrate to `AppNotificationManager`
- For tray-icon-only mode, use `ToastActivationType.Background` with a registered background task, or suppress window focus on foreground activation
