# Windows Reminder v1.0.0 Release Notes

## Overview

Windows Reminder is a Windows toast reminder system powered by Claude Code natural language parsing and a C# WPF tray application. Reminders are stored as Obsidian Markdown files with YAML frontmatter.

## What's New in v1.0.0

### Architecture Rewrite: PowerShell → C# .NET 8 WPF
The entire runtime has been rewritten from a collection of PowerShell scripts into a single C# WPF tray application.

### Key Features
- **System tray icon** with full context menu (scanner controls, startup management, debug tools)
- **Native Windows toasts** with Done / Snooze / Open Note buttons
- **Persistent Snooze** — reschedules by writing to the Markdown file, survives reboot
- **Open Note in Obsidian** — `obsidian://open` integration
- **Auto-start** — registers via Windows Run key
- **DryRun mode** — toast only, no file writes, for testing
- **Single instance protection** via named mutex
- **Serilog file logging** to `%APPDATA%\WindowsReminder\logs`

### Removed
- All PowerShell scripts (`daemon.ps1`, `show-notification.ps1`, `startup-check.ps1`, `protocol-handler.ps1`, etc.)
- `windows-reminder://` custom protocol handler
- BurntToast dependency
- VBS launcher

## Getting Started

```bash
# Build
cd src/WindowsReminder
dotnet publish -c Release -o %LOCALAPPDATA%\WindowsReminder

# Run
%LOCALAPPDATA%\WindowsReminder\WindowsReminder.exe
```

## Requirements
- Windows 10/11
- .NET 8.0 Runtime (or self-contained publish)
- Obsidian with Meta Bind plugin (for inline status dropdown)

## Configuration
`src/WindowsReminder/appsettings.json`

## Reminder Format
Standard Obsidian Markdown with YAML frontmatter:
```markdown
---
status: waiting
trigger: YYYY-MM-DD HH:mm
created: YYYY-MM-DD HH:mm
task_name: xxx
---

# Title

**内容**：Message
```

## Known Limitations
- Toast activation (Done/Snooze callbacks) requires a Start Menu shortcut
- Notifications may be suppressed by Windows Focus Assist
- `Microsoft.Toolkit.Uwp.Notifications` is deprecated by Microsoft; future migration to Windows App SDK planned
