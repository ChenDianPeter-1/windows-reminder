---
name: windows-reminder
description: 在 Windows 系统中创建一次性定时提醒。当用户说"在xxx时候提醒我xxx"、"提醒我"、"设个闹钟"、"到时间叫我"、或表达想在某个时间点收到通知时使用。解析中文时间表达式，通过后台 PowerShell 进程定时触发 BurntToast 原生通知，自动更新 Obsidian 提醒记录。
---

## Windows 定时提醒

工作流程：解析时间 → 创建提醒笔记 → 启动后台进程 → 到时弹出原生通知 + 自动更新状态。

### 第一步：解析时间

| 用户表达 | 解析 |
|---------|------|
| `明天下午3点` | 日期 +1，15:00 |
| `5分钟后` | now +5min |
| `半小时后` | now +30min |
| `1小时后` | now +60min |
| `下周一早上10点` | 下周一 10:00 |
| `晚上7点` | 今天 19:00（已过则警告） |

### 第二步：创建提醒笔记 + 启动后台进程

写临时 .ps1 文件，内容如下模板，然后 `powershell.exe -File` 执行：

```powershell
$trigger = (Get-Date).AddMinutes(1)
$taskName = "ClaudeReminder-<english-id>"
$skillDir = "C:\Users\chenjunjin\.claude\skills\windows-reminder"
$script   = "$skillDir\scripts\show-notification.ps1"
$title    = "<notification title>"
$message  = "<notification content>"
$vaultRoot = "D:\AAAOddsAndEnds\PROGRAM\Obsidian Valut\Study"

# 1. Find the reminders folder (Chinese name — use exclusion to avoid encoding issues)
$exclude = @(".claude", ".claudian", ".obsidian", ".git", "Anki", "wiki", "raw", "output")
$noteDir = Get-ChildItem $vaultRoot -Directory | Where-Object { $exclude -notcontains $_.Name } | Select-Object -First 1
if (-not $noteDir) { Write-Error "提醒 folder not found. Create it manually in vault."; exit 1 }
$noteDir = $noteDir.FullName
$noteDate = $trigger.ToString("yyyy-MM-dd")
$noteName = "$noteDate-$taskName.md"
$notePath = "$noteDir\$noteName"

$noteContent = @"
---
status: waiting
trigger: $($trigger.ToString('yyyy-MM-dd HH:mm'))
created: $(Get-Date -Format 'yyyy-MM-dd HH:mm')
task_name: $taskName
sound: Reminder
snooze: true
---

# $title

` + "`INPUT[inlineSelect(option(waiting, ⏳ 等待中), option(reminded, 🔔 已提醒), option(done, ✅ 已完成), option(pending, ❌ 待完成)):status]`" + @"

**触发时间**：$($trigger.ToString('yyyy年M月d日 HH:mm'))
**内容**：$message
**任务名**：$taskName
"@
$noteContent | Out-File -FilePath $notePath -Encoding UTF8
Write-Host "Note created: $noteName"

# 2. Add legacy table entry (backward compat)
$logFile = Get-ChildItem $vaultRoot -Filter "提醒记录.md" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($logFile) {
    $lines = Get-Content $logFile.FullName -Encoding UTF8
    $lines += "| $(Get-Date -Format 'yyyy-MM-dd HH:mm') | $($trigger.ToString('yyyy-MM-dd HH:mm')) | $message | $taskName | waiting |"
    $lines | Set-Content $logFile.FullName -Encoding UTF8
}

# 3. Launch background timer process
$seconds = [math]::Max(1, [math]::Ceiling(($trigger - (Get-Date)).TotalSeconds))
$bgCmd = "Start-Sleep -Seconds $seconds; & '$script' -Title '$title' -Message '$message' -Sound Reminder -Snooze -NotePath '$notePath' -TaskName '$taskName'"
Start-Process -FilePath "powershell.exe" -ArgumentList @("-NoProfile", "-WindowStyle", "Hidden", "-Command", $bgCmd) -WindowStyle Hidden

Write-Host ("OK: fires at " + $trigger.ToString("HH:mm:ss"))
```

**关键点**：
- `提醒/` 文件夹：每个提醒一个 .md 笔记，YAML frontmatter + Meta Bind 下拉栏
- `status` 属性：waiting → reminded（通知触发时自动更新）
- Meta Bind `INPUT[inlineSelect]`：在笔记内提供状态下拉选择（✅已完成 / ❌待完成 手动改）
- 后台进程：`Start-Process -WindowStyle Hidden` → 无黑框
- 兼容旧格式：同时追加 `提醒记录.md` 表格行

### 第三步：回复

```
✅ 已设置提醒：**YYYY年M月D日 上/下午H:MM** — 内容
📝 提醒笔记：[[提醒/YYYY-MM-DD-ClaudeReminder-xxx]]
```

### 状态说明

| frontmatter 值 | 显示 | 谁改 |
|---------------|------|------|
| `waiting` | ⏳ 等待中 | 系统创建时 |
| `reminded` | 🔔 已提醒 | 系统通知时 |
| `done` | ✅ 已完成 | 手动 |
| `pending` | ❌ 待完成 | 手动 |

### Meta Bind 插件

已安装到 `.obsidian/plugins/obsidian-meta-bind-plugin/`。**需重启 Obsidian 生效**。
