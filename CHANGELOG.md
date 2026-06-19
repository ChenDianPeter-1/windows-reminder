# Changelog

## [1.0.0] — 2026-06-19

### Changed
- **架构迁移**: PowerShell 脚本集 → C# WPF 托盘应用 (.NET 8)
- **核心闭环**: scanner → toast → reminded → Done/Snooze/Open Note
- **Snooze 持久化**: 写回 trigger + status 到 Markdown 文件，重启不丢
- **Open Note**: `obsidian://open` URI 集成
- **开机自启**: C# `AutostartService` 接管注册表 Run 键

### Added
- `src/WindowsReminder/` — C# WPF 托盘应用
  - `Services/AutostartService.cs` — 注册表 Run 键管理
  - `Services/ReminderFileService.cs` — YAML/Markdown 读写
  - `Services/ReminderScannerService.cs` — 后台轮询扫描
  - `Services/ToastNotificationService.cs` — Windows toast 通知
  - `Services/StartMenuShortcutService.cs` — 快捷方式创建
  - `Tray/TrayIconManager.cs` — 系统托盘图标 + 右键菜单
- `tests/` — 测试 Fixtures 和 TestReminders
- `docs/` — 各阶段验证报告

### Removed
- `scripts/daemon.ps1` — 由 `ReminderScannerService` 替代
- `scripts/show-notification.ps1` — 由 `ToastNotificationService` 替代
- `scripts/startup-check.ps1` — 由 `AutostartService` 替代
- `scripts/protocol-handler.ps1` — 由 toast `OnActivated` 事件替代
- `scripts/protocol-launcher.vbs` — 已不需要
- `scripts/register-protocol.ps1` — 已不需要
- `windows-reminder://` 自定义协议 — 已清理

---

历史版本见 git log。
