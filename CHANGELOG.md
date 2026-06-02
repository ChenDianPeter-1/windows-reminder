# Changelog

## [0.4.0] — 2026-06-02

### Fixed
- PowerShell 单引号在某些上下文导致解析失败（`$fmt = 'HH:mm'`）。改用 `[char]34` + 字符串拼接彻底避免嵌套引号问题。
- `-replace` 正则转义陷阱。改用 `.Replace()` 方法。
- SKILL.md 模板同步更新为已验证可行的写法。

### Changed
- VBS 壳生成逻辑：`[char]34` + 拼接替代多层转义字符串。
- SKILL.md 模板精简，移除过时的 `-f` 格式方案。

## [0.3.0] — 2026-06-02

### Added
- **VBS 壳方案**：计划任务不再直接调 `powershell.exe`，改为 `wscript.exe` 跑 VBS 中间层。`Wscript.Shell.Run(..., 0, False)` 的 `0` = 完全隐藏窗口，彻底消除黑框闪现。

### Fixed
- VBS 中双引号需双写（`""`），用 `-replace` 处理。

## [0.2.0] — 2026-06-02

### Changed
- **通知引擎**：自制 WPF/WinForms 弹窗 → [BurntToast](https://github.com/Windos/BurntToast) 原生 Windows toast。通知从灰色方块升级为系统级通知，增加声音、延后按钮、操作中心保留。
- **任务创建**：`schtasks.exe /CREATE` → `Register-ScheduledTask` cmdlet。命令行转义噩梦 → 结构化参数。
- `show-notification.ps1`：137 行（双引擎回退）→ 48 行（纯 BurntToast）。

### Added
- 自动安装 BurntToast 模块（如未安装）。
- `-Snooze` 延后按钮支持。
- `-Sound Reminder` 系统提醒音效。

### Removed
- WPF/WinForms 双引擎回退逻辑。

## [0.1.0] — 2026-06-02

### Added
- 初始版本。解析中文时间表达式（明天下午3点、5分钟后等）。
- Windows 任务计划程序定时触发。
- WPF/WinForms 自制弹窗通知。
- Obsidian vault 提醒记录（`提醒记录.md`）。
- 查看/取消提醒功能。
