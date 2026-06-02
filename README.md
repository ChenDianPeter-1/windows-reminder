# Windows Reminder

A Claude Code skill for Windows toast reminders — no Task Scheduler, no black window flash.

## Architecture

```
User says "在明天下午3点提醒我开会"
  → Parse time
  → Create note in 提醒/ folder (with Meta Bind dropdown)
  → Start-Process background timer
  → At trigger time: BurntToast notification + auto-update status
```

## Features

- Native Windows toast via [BurntToast](https://github.com/Windos/BurntToast)
- `Start-Process` + `Start-Sleep` timing (no Task Scheduler dependency)
- `-WindowStyle Hidden` — zero black window flash
- Auto-update: `status: waiting → reminded` when notification fires
- [Meta Bind](https://github.com/mProjectsCode/obsidian-meta-bind-plugin) inline dropdown for manual status editing
- Obsidian Dataview query for real-time status overview
- Chinese time expression parsing

## Files

| File | Purpose |
|------|---------|
| `SKILL.md` | Claude Code skill definition |
| `scripts/show-notification.ps1` | Toast + frontmatter update |
| `CHANGELOG.md` | Version history |
| `.gitignore` | Ignores temp scripts |

## Vault Setup

- `提醒/` folder — one `.md` note per reminder with YAML frontmatter
- `.obsidian/plugins/obsidian-meta-bind-plugin/` — Meta Bind for dropdown
- `提醒记录.md` — Dataview index

## Dependencies

- Windows 10/11 + PowerShell 5.1+
- [BurntToast](https://github.com/Windos/BurntToast) PowerShell module (auto-installed)
- [Meta Bind](https://github.com/mProjectsCode/obsidian-meta-bind-plugin) Obsidian plugin (bundled in vault)
