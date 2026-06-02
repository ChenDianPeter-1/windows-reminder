# Changelog

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
