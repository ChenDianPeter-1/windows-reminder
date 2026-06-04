---
name: windows-reminder
description: 在 Windows 上创建定时提醒。当用户说"在XXX时候提醒我XXX"、"X分钟后提醒我"、"设个闹钟"、"到时间叫我"时使用。通过后台进程定时弹出 Windows 原生通知，并在 Obsidian 提醒文件夹中创建笔记跟踪状态。
---

## 工作流程

1. **解析时间** — 常见表达：`明天下午3点`(+1d 15:00)、`5分钟后`(+5m)、`半小时后`(+30m)、`1小时后`(+60m)、`下周一早上10点`、`晚上7点`(今天，已过则警告)
   - **秒数规范化**：解析后将秒数归零到整分钟，确保守护进程精准命中。规则：0~30秒→归零分钟不变，31~59秒→归零且分钟+1
2. **Claude 创建笔记** — 用 Write 工具写入 `reminders/YYYY-MM-DD-{slug}.md`
3. **回复用户** — 无需启动后台进程；`daemon.ps1` 每分钟扫描一次，到点自动弹通知

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

## 守护进程 (daemon.ps1)

单个后台进程替代每提醒一个 `Start-Sleep` 进程。每分钟扫描 `reminders/`，到点调 `show-notification.ps1`。

- 通过命名 mutex 防止重复实例
- 启动时自动清理旧的定时进程
- 由 `startup-check.ps1` 在开机时启动

## 回复格式

```
已设提醒：**M月D日 HH:mm** — 内容  [[reminders/YYYY-MM-DD-xxx|查看]]
```

## 状态说明

| 值 | 含义 | 谁改 |
|---|------|------|
| `waiting` | 等待触发 | 系统 |
| `reminded` | 已弹通知 | 系统自动 |
| `done` | 已完成 | 点 toast "完成"按钮 / 手动下拉 |
| `pending` | 待完成 | 手动 |

## Toast 操作

通知弹窗同时提供原生 **Snooze 下拉**（1/5/10/30/60 分钟）和 **Done 按钮**。

- Snooze：Windows 原生延后提醒，到时自动重弹通知
- Done：通过 `windows-reminder://done?note=xxx` 自定义协议 → VBS 壳（无黑框）→ `protocol-handler.ps1` → `status: done`
- Toast 用 `New-BTContent` + `Submit-BTNotification` 手动构建，绕开 BurntToast 高層 API 的互斥限制

- `register-protocol.ps1` — 注册表注册协议 + VBS 壳（自愈）
- `protocol-launcher.vbs` — 隐藏 PowerShell 窗口
- `protocol-handler.ps1` — 解析 URL，更新 status

## 开机补漏

`startup-check.ps1` 通过注册表 Run 键自注册（`HKCU\...\Run\WindowsReminder`）。每次开机自动扫描 `reminders/`，补弹错过时间的通知，并更新状态。脚本自带自愈——每次运行检查注册表，丢失自动补上。

## 架构原则

- **笔记由 Claude 写，定时由 daemon.ps1 做** — 零后台进程开销
- `daemon.ps1` 单一守护进程，每分钟轮询，无论多少提醒只有一个进程
- `show-notification.ps1` 触发时自动改 `status: waiting → reminded`
- `startup-check.ps1` 开机补漏 + 启动守护 + 自注册 + 协议注册
