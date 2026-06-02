# Windows Reminder

A Claude Code skill that creates native Windows toast reminders via Task Scheduler + BurntToast.

## How it works

1. User says "在明天下午3点提醒我开会"
2. Skill parses the Chinese time expression
3. Creates a VBS wrapper → Windows scheduled task → BurntToast notification
4. Logs to an Obsidian vault note

## Features

- Native Windows 10/11 toast notifications (BurntToast)
- Zero black window flash (VBS shell wrapper)
- Snooze & dismiss support
- System reminder sound
- Obsidian vault logging

## Files

- `SKILL.md` — skill definition for Claude Code
- `scripts/show-notification.ps1` — notification script (BurntToast)
- `.gitignore` — ignores per-reminder temp files

## Dependencies

- Windows 10/11
- PowerShell 5.1+
- BurntToast module (auto-installed if missing)
