# Changelog

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
