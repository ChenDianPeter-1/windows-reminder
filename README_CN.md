<p align="center">
  <h1 align="center">Windows Reminder</h1>
  <p align="center"><em>用自然语言设 Windows 定时提醒 — Claude Code + Obsidian + C# WPF 托盘</em></p>
</p>

<p align="center">
  <a href="./LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6?logo=windows" alt="Platform">
</p>

<p align="center">
  <a href="./README.md">English</a>
</p>

---

## 工作原理

```
你说："十二点提醒我吃饭"
              │
     ┌────────▼────────┐
     │  Claude Code     │  解析时间 → 创建 reminders/YYYY-MM-DD-lunch.md
     │  (SKILL.md)      │
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  WindowsReminder │  C# WPF 托盘程序每 30 秒扫描
     │  .exe（托盘）     │  → 到期 → Windows toast
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  Toast           │  🔔 Done → status: done
     │  [Done] [Snooze] │  ⏰ Snooze → trigger 推迟
     │  [Open Note]     │  📝 Open Note → Obsidian
     └──────────────────┘
```

## 功能

- 🗣️ **自然语言** — "提醒我明天下午3点开会" → 设好提醒
- 🔔 **Windows 原生 toast** — Done / Snooze / Open Note 按钮
- 📝 **Obsidian Markdown** — 提醒存为 YAML frontmatter 笔记
- 🔂 **单一托盘程序** — 一个进程，管理所有提醒
- ✅ **一键完成** — 点 Done 自动写 done
- ⏰ **持久化 Snooze** — 推迟写入文件，重启不丢
- 📖 **Open Note** — 通过 `obsidian://open` 打开笔记
- 🔁 **开机自启** — 注册表 Run 键
- 🛡️ **DryRun 模式** — 测试时不写文件

## 快速开始

```bash
# 1. 安装 .NET 8.0 SDK
winget install Microsoft.DotNet.SDK.8

# 2. 运行
cd src/WindowsReminder
dotnet run

# 或直接运行发布版：
%LOCALAPPDATA%\WindowsReminder\WindowsReminder.exe

# 3. 发布（独立 EXE）
dotnet publish -c Release
```

## 配置

`src/WindowsReminder/appsettings.json`:

| 键 | 说明 |
|----|------|
| `VaultRoot` | Obsidian vault 根路径 |
| `RemindersRelativePath` | 提醒文件夹（默认 `reminders`） |
| `ObsidianVaultName` | vault 名称 |
| `PollIntervalSeconds` | 扫描间隔（默认 30） |
| `DryRun` | 只弹 toast，不写文件 |
| `AutoStart` | 开机自启 |

## 提醒文件格式

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

## 托盘菜单

- Test Toast / Open Log Folder / Open Reminders Folder
- Scanner: Scan Now / Status / Pause / Resume
- Startup: Enable / Disable / Status
- Debug: Parse / WriteBack Test

## 故障排查

| 问题 | 解决 |
|------|------|
| toast 不弹 | 检查 Windows 通知设置；确认开始菜单快捷方式存在 |
| Done 按钮无效 | 确认 `%APPDATA%\Microsoft\Windows\Start Menu\Programs\WindowsReminder.lnk` |
| 旧 daemon 残留 | 删除 `HKCU\...\Run\WindowsReminder`（PowerShell 条目） |
| 程序无法启动 | 需要 .NET 8 运行时；检查 `%APPDATA%\WindowsReminder\logs\` |

## 许可证

MIT © ChenDianPeter-1
