---
name: windows-reminder
description: 在 Windows 上创建定时提醒。当用户说"在XXX时候提醒我XXX"、"X分钟后提醒我"、"设个闹钟"、"到时间叫我"时使用。提醒由 WindowsReminder.exe 后台托盘程序扫描并弹 Windows 原生通知。
---

## 工作流程

1. **解析时间** — 常见表达：`明天下午3点`(+1d 15:00)、`5分钟后`(+5m)、`半小时后`(+30m)、`1小时后`(+60m)、`下周一早上10点`、`晚上7点`(今天，已过则警告)
   - **秒数规范化**：解析后将秒数归零到整分钟。规则：0~30秒→归零分钟不变，31~59秒→归零且分钟+1
2. **Claude 创建笔记** — 用 Write 工具写入 `reminders/YYYY-MM-DD-{slug}.md`
3. **回复用户** — 无需启动任何进程； WindowsReminder.exe 托盘程序每分钟扫描一次，到点自动弹通知

## 笔记模板

```markdown
---
status: waiting
trigger: YYYY-MM-DD HH:mm
created: YYYY-MM-DD HH:mm
task_name: ClaudeReminder-xxx
---

# 标题

`INPUT[inlineSelect(option(waiting, ⏳ 等待中), option(reminded, 🔔 已提醒), option(done, ✅ 已完成), option(pending, ❌ 待完成)):status]`

**触发时间**：YYYY年M月D日 HH:mm
**内容**：xxx
```

`INPUT[inlineSelect]` 提供状态下拉栏（需 Meta Bind 插件，已安装）。

## 回复格式

```
已设提醒：**M月D日 HH:mm** — 内容  [[reminders/YYYY-MM-DD-xxx|查看]]
```

## 运行时架构

### Claude Code 职责
- 解析自然语言时间
- 创建/修改 `reminders/*.md` Markdown 文件
- 只改 Markdown 文件，不改 C# 程序状态

### WindowsReminder.exe 职责（后台托盘程序）
- 系统托盘图标 + 右键菜单
- 每分钟扫描 `reminders/` 文件夹
- 到时间弹 Windows 原生 toast（带 Done / Snooze 5m / Snooze 15m / Open Note 按钮）
- Done → 自动将 status 改为 done
- Snooze → 自动将 status 改回 waiting + trigger 推迟
- Open Note → 在 Obsidian 中打开对应笔记
- 开机自启（注册表 Run 键）
- 日志输出到 `%APPDATA%\WindowsReminder\logs\`

## 状态说明

| 值 | 含义 | 谁改 |
|---|------|------|
| `waiting` | 等待触发 | Claude 创建 / Snooze 后自动设置 |
| `reminded` | 已弹通知 | WindowsReminder.exe 自动 |
| `done` | 已完成 | 点 toast "Done" 按钮 / 手动下拉 |
| `pending` | 待完成 | 手动 |

## 配置

`src/WindowsReminder/appsettings.json`:
- `VaultRoot` — Obsidian vault 根路径
- `RemindersRelativePath` — reminders 文件夹相对路径
- `ObsidianVaultName` — Obsidian vault 名称
- `PollIntervalSeconds` — 扫描间隔
- `DryRun` — true 时不写回文件
- `AutoStart` — 开机自启

## 托盘菜单

- About
- Test Toast
- Open Log Folder
- Open Reminders Folder
- Scanner > Scan Now / Status / Pause / Resume
- Startup > Enable / Disable / Status
- Debug > Parse / WriteBack Test
- Exit

## 启动方式

```
# 开发
cd src/WindowsReminder
dotnet run

# 发布
dotnet publish -c Release
./bin/Release/net8.0-windows10.0.19041.0/publish/WindowsReminder.exe
```
