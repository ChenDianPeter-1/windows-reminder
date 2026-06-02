---
name: windows-reminder
description: 在 Windows 系统中创建一次性定时提醒。当用户说"在xxx时候提醒我xxx"、"提醒我"、"设个闹钟"、"到时间叫我"、或表达想在某个时间点收到通知时使用。解析中文时间表达式并通过 Windows 任务计划程序 + BurntToast 模块弹出原生 toast 通知（带声音 + 可延后），无黑框闪现。
---

## Windows 定时提醒

工作流程：解析时间 → 写 VBS 壳脚本 → 创建计划任务 → 记录 Obsidian → 回复。

### 第一步：解析时间

| 用户表达 | 解析 |
|---------|------|
| `明天下午3点` | 日期 +1，时间 15:00 |
| `5分钟后` | now +5min |
| `半小时后` | now +30min |
| `1小时后` | now +60min |
| `下周一早上10点` | 下周一 10:00 |
| `6月5号下午2点` | 当年 6月5日 14:00 |
| `晚上7点` | 今天 19:00（已过则警告） |

### 第二步：创建计划任务（VBS 壳方案）

计划任务直接调 `powershell.exe` 会产生黑框闪现。方案：用 VBS 壳做中间层——计划任务 → `wscript.exe` 跑 .vbs → .vbs 隐藏窗口启 PowerShell → BurntToast 通知。

写临时 .ps1 文件，内容如下模板，然后 `powershell.exe -File` 执行：

```powershell
$trigger = (Get-Date).AddMinutes(1)          # 或用 Get-Date -Year ... -Month ... -Day ...
$taskName = "ClaudeReminder-<english-id>"
$skillDir = "C:\Users\chenjunjin\.claude\skills\windows-reminder"
$script   = "$skillDir\scripts\show-notification.ps1"
$title    = "<notification title>"
$message  = "<notification content>"

Write-Host ("Scheduling for " + $trigger.ToString("HH:mm") + "...")

# Build PowerShell command line. Backtick-doublequote (`") for literal " inside double-quoted string.
$psCmd = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$script`" -Title `"$title`" -Message `"$message`" -Sound Reminder -Snooze"

# VBS requires doubled double quotes. .Replace() is simpler than -replace (no regex escaping issues).
$psCmdForVbs = $psCmd.Replace('"', '""')

# Build VBS wrapper using [char]34 + concatenation (avoids nested quote hell)
$q = [char]34
$vbsContent = "CreateObject(" + $q + "Wscript.Shell" + $q + ").Run " + $q + $psCmdForVbs + $q + ", 0, False"
$vbsPath = "$skillDir\scripts\_w_$taskName.vbs"
$vbsContent | Out-File -FilePath $vbsPath -Encoding ASCII

# Create scheduled task
$t = New-ScheduledTaskTrigger -Once -At $trigger
$a = New-ScheduledTaskAction -Execute "wscript.exe" -Argument ($q + $vbsPath + $q)
Register-ScheduledTask -TaskName $taskName -Trigger $t -Action $a -Force -ErrorAction Stop | Out-Null

Write-Host "OK"
```

**关键点**：
- `[char]34` = 双引号字符，用拼接避免嵌套转义
- `.Replace('"', '""')` 把 `"` 变成 `""`（VBS 语法要求），比 `-replace` 简单
- 回勾双引号 `` `" `` 在双引号字符串中表示字面 `"`
- `"Wscript.Shell".Run(..., 0, False)` 的 `0` = 隐藏窗口
- VBS 文件放 `scripts\_w_<任务名>.vbs`，删除任务时一并清理

### 第三步：验证

```powershell
schtasks.exe /QUERY /TN "ClaudeReminder-<name>" /FO LIST
```

### 第四步：Obsidian 记录

追加到 vault 根目录 `提醒记录.md`：

```markdown
| YYYY-MM-DD HH:MM | YYYY-MM-DD HH:MM | 内容 | ClaudeReminder-xxx | ⏳等待中 |
```

### 第五步：回复

```
✅ 已设置提醒：**YYYY年M月D日 上/下午H:MM** — 内容
到时间弹出原生通知（带声音 + 可延后，无黑框闪现）
取消：说"取消提醒 xxx"
```

### 查看/删除提醒

查看：`schtasks.exe /QUERY /FO LIST | Select-String "ClaudeReminder"`

删除：
```powershell
schtasks.exe /DELETE /TN "任务名" /F
Remove-Item "C:\Users\chenjunjin\.claude\skills\windows-reminder\scripts\_w_任务名.vbs" -Force
```
更新 `提醒记录.md` 状态为 `❌已取消`。

### 架构决策备忘

| 问题 | 方案 | 理由 |
|------|------|------|
| 通知怎么显示 | BurntToast `New-BurntToastNotification` | 原生 toast，1709⭐ 项目 |
| 怎么定时 | `Register-ScheduledTask` | 结构化传参，比 `schtasks.exe /CREATE` 可靠 |
| 黑框闪现 | VBS 壳 → `Wscript.Shell.Run(..., 0)` | 0=隐藏窗口，业界标准做法 |
| VBS 引号转义 | `-replace '"', '""'` | VBS 中双引号需双写，用替换避免手动拼接 |
| 移除引号嵌套 | 字符串拼接 `+` + `-replace` | 不用多层转义 |
