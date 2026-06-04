# Changelog

## [0.7.1] — 2026-06-04

### Fixed
- **守护进程重复实例**：mutex 改为 `$createdNew` 模式，防止两个 daemon 同时运行。
- **Windows 通知被压制**：PowerShell 不在通知白名单里，导致 toast 可能不显示。`startup-check.ps1` 开机时自动注册 `HKCU\...\Notifications\Settings\Windows PowerShell`。

### Added
- `daemon.ps1` 日志功能：`%TEMP%\windows-reminder-daemon.log`，记录启动、触发、错误。

## [0.7.0] — 2026-06-04

### Changed
- **守护进程架构**：不再为每个提醒启动独立 PowerShell 进程 `Start-Sleep`。改为 `daemon.ps1` 单一后台进程，每分钟轮询 `reminders/`，到点触发通知。无论多少提醒只占一个进程。
- `startup-check.ps1` 开机补漏后自动启动守护进程。
- `protocol-handler.ps1` 的 snooze 改为重置 status 为 `waiting`，由守护进程下一次轮询触发。
- SKILL.md 工作流程简化：删除"启动后台定时"步骤，改为纯创建笔记。

### Removed
- 每提醒一个 `Start-Sleep` 后台进程的方案（旧定时脚本模板）。

## [0.6.1] — 2026-06-03

### Changed
- **原生 Snooze 下拉 + Done 按钮共存**：用 `New-BTContent` + `Submit-BTNotification` 手动构建 toast，突破 BurntToast 高層 API 的 `-Button`/`-SnoozeAndDismiss` 互斥限制。现在通知同时有：snooze 时间下拉（1/5/10/30/60 min）+ Done 按钮。
- `show-notification.ps1` 重构为自定义 toast 构建。
- `startup-check.ps1` 改为调用 `show-notification.ps1`，复用同一套 toast 逻辑。

### Fixed
- 按钮中文 "完成" 乱码 → 改为英文 "Done"。
- 点击按钮弹出黑框 → 协议注册改用 `wscript.exe` + VBS 壳（`protocol-launcher.vbs`）彻底隐藏 PowerShell 窗口。

## [0.6.0] — 2026-06-03

### Added
- **Toast "完成"按钮**：点击通知上的"完成"直接更新笔记状态为 `done`，无需切回 Obsidian。
  实现：自定义协议 `windows-reminder://done?note=xxx`，点击按钮 → 协议处理器 → 更新 frontmatter。
- `scripts/protocol-handler.ps1` — 解析协议 URL，更新笔记状态。
- `scripts/register-protocol.ps1` — 注册 `windows-reminder://` 协议到注册表，自愈机制。
- `startup-check.ps1` 和 `show-notification.ps1` 启动时自动注册协议。

### Changed
- `show-notification.ps1`：默认使用"完成"按钮代替 Snooze（二者互斥）。`-Snooze` 开关仍可用。
- 定时脚本模板：`$bgCmd` 不再传 `-Snooze`。

## [0.5.1] — 2026-06-03

### Fixed
- **命令行中文编码崩溃**：`Start-Process -WindowStyle Hidden -Command "..."` 中，中文参数会被 PowerShell 5.1 的 Hidden 窗口编码搞坏，导致后台定时进程静默失败。修复：`show-notification.ps1` 改为从笔记文件内部读取标题和内容（`Get-Content -Encoding UTF8`），定时脚本的 `$bgCmd` 保持纯 ASCII。
- 定时脚本模板同步更新：去掉 `$title`/`$message` 变量和 `-Title`/`-Message` 参数。

### Added
- 双语 README：`README.md`（English）+ `README_CN.md`（中文），顶部一键切换。

## [0.5.0] — 2026-06-03

### Changed
- **定时机制**：Windows Task Scheduler → `Start-Process` + `Start-Sleep` 后台进程。Task Scheduler 在开发机上不稳定（所有任务不执行，原因不明），改用具进程方案零系统依赖。
- **数据模型**：单 `提醒记录.md` 表格 → `提醒/` 文件夹每提醒一笔记 + YAML frontmatter。
- **去黑框**：不再需要 VBS 壳，`Start-Process -WindowStyle Hidden` 直接消除闪现。

### Added
- Meta Bind 插件（`obsidian-meta-bind-plugin` v1.4.15），状态下拉栏 `INPUT[inlineSelect]`。
- `提醒/提醒模板.md` 模板笔记。
- `提醒记录.md` 增加 Dataview 实时状态视图。
- 通知触发时自动更新笔记 frontmatter `status: waiting → reminded`，同时兼容旧表格格式。

### Removed
- VBS 壳方案（`CreateObject("Wscript.Shell").Run`）。
- `Register-ScheduledTask` / `schtasks.exe /CREATE` 依赖。

## [0.4.0] — 2026-06-02

### Fixed
- PowerShell 单引号解析失败改用 `[char]34` + `.Replace()`。
- `$().ToString('...')` 子表达式引号解析失败。

## [0.3.0] — 2026-06-02

### Added
- VBS 壳方案消除黑框闪现。

## [0.2.0] — 2026-06-02

### Changed
- WPF/WinForms 弹窗 → BurntToast 原生 toast。
- `schtasks.exe /CREATE` → `Register-ScheduledTask`。

## [0.1.0] — 2026-06-02

### Added
- 初始版本，中文时间解析 + Windows 计划任务 + 自制弹窗。
