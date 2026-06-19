<p align="center">
  <h1 align="center">Windows Reminder</h1>
  <p align="center"><em>Set Windows toast reminders via Claude Code + Obsidian — powered by C# WPF tray app</em></p>
</p>

<p align="center">
  <a href="./LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6?logo=windows" alt="Platform">
</p>

<p align="center">
  <a href="./README_CN.md">中文版</a>
</p>

---

## How It Works

```
You say: "十二点提醒我吃饭"
              │
     ┌────────▼────────┐
     │  Claude Code     │  Parses time → creates reminders/YYYY-MM-DD-lunch.md
     │  (SKILL.md)      │
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  WindowsReminder │  C# WPF tray app polls every 30s
     │  .exe (tray)     │  → trigger reached → Windows toast
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  Toast           │  🔔 Done → status: done
     │  [Done] [Snooze] │  ⏰ Snooze → trigger postponed
     │  [Open Note]     │  📝 Open Note → Obsidian
     └──────────────────┘
```

## Features

- 🗣️ **Natural language** — "提醒我明天下午3点开会" → sets a toast
- 🔔 **Native Windows toasts** — Done / Snooze / Open Note buttons
- 📝 **Obsidian Markdown** — reminders stored as YAML frontmatter notes
- 🔂 **Single tray app** — one process, all reminders
- ✅ **One-click Done** — marks file as done
- ⏰ **Persistent Snooze** — reschedules trigger in file, survives reboot
- 📖 **Open Note** — opens reminder in Obsidian via `obsidian://open`
- 🔁 **Auto-start** — registers via Windows Run key
- 🛡️ **DryRun mode** — test without modifying files

## Quick Start

```bash
# 1. Install .NET 8.0 SDK
winget install Microsoft.DotNet.SDK.8

# 2. Run
cd src/WindowsReminder
dotnet run

# 3. Publish (standalone EXE)
dotnet publish -c Release
```

## Configuration

`src/WindowsReminder/appsettings.json`:

| Key | Description |
|-----|-------------|
| `VaultRoot` | Obsidian vault root path |
| `RemindersRelativePath` | Reminders folder (default: `reminders`) |
| `ObsidianVaultName` | Vault name for `obsidian://open` |
| `PollIntervalSeconds` | Scan interval (default: 30) |
| `DryRun` | Toast only, no file writes |
| `AutoStart` | Register for Windows startup |

## Reminder File Format

```markdown
---
status: waiting
trigger: 2026-06-03 12:00
created: 2026-06-03 11:08
task_name: ClaudeReminder-lunch
---

# 吃饭提醒

**触发时间**：2026年6月3日 12:00
**内容**：该吃饭了！
```

## Tray Menu

- Test Toast / Open Log Folder / Open Reminders Folder
- Scanner: Scan Now / Status / Pause / Resume
- Startup: Enable / Disable / Status
- Debug: Parse / WriteBack Test

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Toast not showing | Check Windows notification settings; ensure Start Menu shortcut exists |
| Done button no callback | Verify `%APPDATA%\Microsoft\Windows\Start Menu\Programs\WindowsReminder.lnk` exists |
| Old daemon still running | Remove `HKCU\...\Run\WindowsReminder` (PowerShell entry) |
| App won't start | .NET 8 runtime required; check `%APPDATA%\WindowsReminder\logs\` |

## License

MIT © ChenDianPeter-1
