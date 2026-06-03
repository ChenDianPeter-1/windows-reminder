<p align="center">
  <img src="https://img.icons8.com/fluency/96/appointment-reminders--v1.png" alt="Windows Reminder" width="96" height="96">
</p>

<h1 align="center">Windows Reminder</h1>

<p align="center">
  <em>Set Windows toast reminders using natural language — powered by Claude Code + Obsidian</em>
</p>

<p align="center">
  <a href="https://github.com/ChenDianPeter-1/windows-reminder/blob/master/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <a href="https://github.com/ChenDianPeter-1/windows-reminder/stargazers"><img src="https://img.shields.io/github/stars/ChenDianPeter-1/windows-reminder?style=flat" alt="Stars"></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6?logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/powershell-5.1%2B-5391FE?logo=powershell" alt="PowerShell">
</p>

<p align="center">
  <a href="./README_CN.md">中文版</a>
</p>

---

## ✨ Features

- 🗣️ **Natural language** — "提醒我明天下午3点开会" → sets a toast notification
- 🔔 **Native Windows toasts** — via [BurntToast](https://github.com/Windos/BurntToast), no custom UI
- 👻 **No black window flash** — `Start-Process -WindowStyle Hidden` all the way down
- 📝 **Obsidian-native logging** — each reminder is a Markdown note with YAML frontmatter
- 🔄 **Auto status update** — `waiting → reminded` when the toast fires, via regex in `show-notification.ps1`
- ⏬ **Inline status dropdown** — powered by [Meta Bind](https://github.com/mProjectsCode/obsidian-meta-bind-plugin) `INPUT[inlineSelect]`
- 📊 **Live Dataview** — query all reminders in one table
- 🔁 **Startup recovery** — `startup-check.ps1` self-registers via registry Run key; catches missed reminders after reboot
- 🧹 **No Task Scheduler** — pure `Start-Process` + `Start-Sleep` background timing, zero system dependencies
- 🛡️ **Self-healing** — startup script re-registers itself if the registry key is ever removed

## 🏗️ How It Works

```
You say: "十二点提醒我吃饭"
              │
     ┌────────▼────────┐
     │  SKILL.md        │  Claude parses time → 2026-06-03 12:00
     │  Parse & create  │
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  reminders/      │  Writes YYYY-MM-DD-slug.md with YAML frontmatter
     │  2026-06-03-     │    status: waiting
     │  lunch.md        │    trigger: 2026-06-03 12:00
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  Timer .ps1      │  Start-Process powershell.exe -WindowStyle Hidden
     │  Start-Sleep     │    → sleeps until trigger time
     │  + bg process    │    → calls show-notification.ps1
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  BurntToast      │  🔔 Native Windows notification
     │  notification    │  📝 status: waiting → reminded (auto)
     │  + status update │  ⏬ Optional: inlineSelect → done/pending
     └──────────────────┘
```

## 📦 Installation

### 1. Install the Skill

```bash
# Clone into your Claude skills directory
git clone https://github.com/ChenDianPeter-1/windows-reminder.git \
  "$HOME/.claude/skills/windows-reminder"
```

### 2. Install BurntToast (auto-installed on first run)

```powershell
Install-Module -Name BurntToast -Force -Scope CurrentUser
```

### 3. Obsidian Plugins (already configured in vault)

- [Meta Bind](https://github.com/mProjectsCode/obsidian-meta-bind-plugin) — status dropdown
- [Dataview](https://github.com/blacksmithgu/obsidian-dataview) — reminder overview table

### 4. Startup Recovery (one-time setup)

Run once to register the startup check:

```powershell
& "$env:USERPROFILE\.claude\skills\windows-reminder\scripts\startup-check.ps1"
```

This adds a `WindowsReminder` entry to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. It runs at every login and re-registers itself if missing.

## 🚀 Usage

Just say it naturally to Claude Code in your Obsidian vault:

| You say | Result |
|---------|--------|
| `十二点提醒我吃饭` | Toast at 12:00 today |
| `5分钟后提醒我看邮件` | Toast in 5 minutes |
| `明天下午3点提醒我开会` | Toast tomorrow at 15:00 |
| `下周一早上10点提醒我交报告` | Toast next Monday at 10:00 |
| `晚上7点提醒我去健身房` | Toast at 19:00 today (warns if already past) |

## 📁 Project Structure

```
windows-reminder/
├── SKILL.md                          # Claude Code skill definition
├── scripts/
│   ├── show-notification.ps1         # Toast + frontmatter status update
│   └── startup-check.ps1             # Post-reboot missed-reminder scan
├── CHANGELOG.md                      # Version history
├── README.md                         # This file (English)
├── README_CN.md                      # Chinese version
└── .gitignore
```

### Vault-side (created by the skill at runtime)

```
vault/
├── reminders/                        # One .md note per reminder
│   ├── 2026-06-03-lunch.md
│   └── 2026-06-04-meeting.md
└── 提醒记录.md                       # Dataview overview query
```

## 📝 Reminder Note Format

```markdown
---
status: waiting
trigger: 2026-06-03 12:00
created: 2026-06-03 11:08
task_name: ClaudeReminder-lunch
---

# 吃饭提醒

`INPUT[inlineSelect(option(waiting, ⏳ 等待中), option(reminded, 🔔 已提醒),
  option(done, ✅ 已完成), option(pending, ❌ 待完成)):status]`

**触发时间**：2026年6月3日 12:00
**内容**：该吃饭了！
```

## 🔧 Status States

| Status | Meaning | Updated by |
|--------|---------|------------|
| `waiting` | Pending trigger | System (on creation) |
| `reminded` | Notification fired | System (auto) |
| `done` | Completed | User (manual dropdown) |
| `pending` | To-do / snoozed | User (manual dropdown) |

## ⚠️ Known Limitations

- **PowerShell 5.1 encoding**: `.ps1` files must be pure ASCII or UTF-8 with BOM. The skill handles this internally — timer scripts never contain Chinese characters; `show-notification.ps1` reads them from the note file instead.
- **No persistent scheduled tasks**: timers are one-shot background processes. If you kill the PowerShell process, the reminder is lost until `startup-check.ps1` catches it on next boot.
- **Single machine**: reminders are local. No sync between devices.

## 📄 License

MIT © ChenDianPeter-1

---

<p align="center">
  <sub>Built with ❤️ for the Obsidian + Claude Code ecosystem</sub>
</p>
