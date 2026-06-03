---
name: windows-reminder
description: 在 Windows 上创建定时提醒。当用户说"在XXX时候提醒我XXX"、"X分钟后提醒我"、"设个闹钟"、"到时间叫我"时使用。通过后台进程定时弹出 Windows 原生通知，并在 Obsidian 提醒文件夹中创建笔记跟踪状态。
---

## 工作流程

1. **解析时间** — 常见表达：`明天下午3点`(+1d 15:00)、`5分钟后`(+5m)、`半小时后`(+30m)、`1小时后`(+60m)、`下周一早上10点`、`晚上7点`(今天，已过则警告)
2. **Claude 创建笔记** — 用 Write 工具写入 `reminders/YYYY-MM-DD-{slug}.md`，保证中文/emoji 编码正确
3. **启动后台定时** — 写纯 ASCII 的 .ps1，`Start-Sleep` 到点后调 `show-notification.ps1` 弹 toast + 更新状态
4. **回复用户**

## 笔记模板

```markdown
---
status: waiting
trigger: YYYY-MM-DD HH:mm
created: YYYY-MM-DD HH:mm
task_name: ClaudeReminder-xxx
---

# 标题

`INPUT[inlineSelect(option(waiting, 等待中), option(reminded, 已提醒), option(done, 已完成), option(pending, 待完成)):status]`

**触发时间**：YYYY年M月D日 HH:mm
**内容**：xxx
```

`INPUT[inlineSelect]` 提供状态下拉栏（需 Meta Bind 插件，已安装）。

## 定时脚本模板

用 **bash heredoc** 写临时 .ps1（纯 ASCII，不含中文），然后 `powershell.exe -File` 执行。
`show-notification.ps1` 会自己从笔记里读标题/内容，定时脚本只需传 `-NotePath`。

```powershell
$seconds = <N>
$noteName = '<YYYY-MM-DD-xxx.md>'
$taskName = 'ClaudeReminder-xxx'
$script   = 'C:\Users\chenjunjin\.claude\skills\windows-reminder\scripts\show-notification.ps1'
$vaultRoot = 'D:\AAAOddsAndEnds\PROGRAM\Obsidian Valut\Study'

# 按文件名递归搜索
$notePath = Get-ChildItem $vaultRoot -Recurse -Filter $noteName -ErrorAction SilentlyContinue | Select-Object -First 1 | ForEach-Object { $_.FullName }
if (-not $notePath) { Write-Error "Note not found: $noteName"; exit 1 }

# bgCmd 不含中文——show-notification.ps1 会自己从笔记文件读标题/内容
$bgCmd = "Start-Sleep -Seconds $seconds; & '$script' -Sound Reminder -Snooze -NotePath '$notePath' -TaskName '$taskName'"
Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-WindowStyle', 'Hidden', '-Command', $bgCmd) -WindowStyle Hidden
```

## 回复格式

```
已设提醒：**M月D日 HH:mm** — 内容  [[reminders/YYYY-MM-DD-xxx|查看]]
```

## 状态说明

| 值 | 含义 | 谁改 |
|---|------|------|
| `waiting` | 等待触发 | 系统 |
| `reminded` | 已弹通知 | 系统自动 |
| `done` | 已完成 | 手动 |
| `pending` | 待完成 | 手动 |

## 开机补漏

`startup-check.ps1` 通过注册表 Run 键自注册（`HKCU\...\Run\WindowsReminder`）。每次开机自动扫描 `reminders/`，补弹错过时间的通知，并更新状态。脚本自带自愈——每次运行检查注册表，丢失自动补上。

## 架构原则

- 笔记由 Claude 写，定时由 .ps1 做，两件事严格分离
- `show-notification.ps1` 触发时自动改 `status: waiting → reminded`
- `startup-check.ps1` 开机补漏，不丢提醒
