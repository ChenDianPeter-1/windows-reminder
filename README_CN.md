<p align="center">
  <img src="https://img.icons8.com/fluency/96/appointment-reminders--v1.png" alt="Windows Reminder" width="96" height="96">
</p>

<h1 align="center">Windows Reminder</h1>

<p align="center">
  <em>用自然语言设 Windows 定时提醒 — Claude Code + Obsidian 联动</em>
</p>

<p align="center">
  <a href="https://github.com/ChenDianPeter-1/windows-reminder/blob/master/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <a href="https://github.com/ChenDianPeter-1/windows-reminder/stargazers"><img src="https://img.shields.io/github/stars/ChenDianPeter-1/windows-reminder?style=flat" alt="Stars"></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6?logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/powershell-5.1%2B-5391FE?logo=powershell" alt="PowerShell">
</p>

<p align="center">
  <a href="./README.md">English</a>
</p>

---

## ✨ 特性

- 🗣️ **自然语言输入** — "提醒我明天下午3点开会" → 自动设好定时通知
- 🔔 **Windows 原生通知** — 基于 [BurntToast](https://github.com/Windos/BurntToast)，不用自己画 UI
- 👻 **无黑框闪现** — 全链路 `Start-Process -WindowStyle Hidden`
- 📝 **Obsidian 原生记录** — 每个提醒一张 Markdown 笔记，YAML frontmatter 管理状态
- 🔄 **状态自动更新** — 通知触发时，`show-notification.ps1` 用正则自动把 `waiting → reminded`
- ✅ **一键"完成"** — 通知上有"完成"按钮；点了自动把状态改成 `done`，不用切 Obsidian
- ⏬ **行内状态下拉栏** — 基于 [Meta Bind](https://github.com/mProjectsCode/obsidian-meta-bind-plugin) 的 `INPUT[inlineSelect]`
- 📊 **Dataview 实时视图** — 一个表格看所有提醒
- 🔁 **开机补漏** — `startup-check.ps1` 通过注册表 Run 键自注册；开机自动扫描过期提醒并补弹通知
- 🔂 **单一守护进程** — 一个后台进程每分钟轮询，所有提醒共用（不再每提醒一个进程）
- 🛡️ **自愈机制** — 开机脚本和协议处理器的注册表项丢失都能自己补回来

## 🏗️ 工作原理

```
你说："十二点提醒我吃饭"
              │
     ┌────────▼────────┐
     │  SKILL.md        │  Claude 解析时间 → 2026-06-03 12:00
     │  解析 + 创建笔记  │
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  reminders/      │  写入 YYYY-MM-DD-slug.md，带 YAML frontmatter
     │  2026-06-03-     │    status: waiting
     │  lunch.md        │    trigger: 2026-06-03 12:00
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  daemon.ps1      │  单进程每分钟轮询
     │  （后台守护）     │    → 触发时间到了？
     │                  │    → 调用 show-notification.ps1
     └────────┬────────┘
              │
     ┌────────▼────────┐
     │  BurntToast      │  🔔 Windows 原生通知弹出
     │  通知 + 状态更新  │  📝 status: waiting → reminded（自动）
     │                  │  ⏬ 可选：手动 inlineSelect → done/pending
     └──────────────────┘
```

## 📦 安装

### 1. 安装 Skill

```bash
# 克隆到 Claude skills 目录
git clone https://github.com/ChenDianPeter-1/windows-reminder.git \
  "$HOME/.claude/skills/windows-reminder"
```

### 2. 安装 BurntToast（首次运行时会自动安装）

```powershell
Install-Module -Name BurntToast -Force -Scope CurrentUser
```

### 3. Obsidian 插件（已在 vault 中配置）

- [Meta Bind](https://github.com/mProjectsCode/obsidian-meta-bind-plugin) — 状态下拉栏
- [Dataview](https://github.com/blacksmithgu/obsidian-dataview) — 提醒概览表格

### 4. 开机补漏（一次性设置）

运行一次即可注册开机自启：

```powershell
& "$env:USERPROFILE\.claude\skills\windows-reminder\scripts\startup-check.ps1"
```

这会在注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 中添加 `WindowsReminder` 项。每次开机自动运行，注册表丢失也能自己补回来。

## 🚀 使用方式

在 Obsidian vault 中对着 Claude Code 直接说人话：

| 你说 | 效果 |
|------|------|
| `十二点提醒我吃饭` | 今天 12:00 弹通知 |
| `5分钟后提醒我看邮件` | 5 分钟后弹通知 |
| `明天下午3点提醒我开会` | 明天 15:00 弹通知 |
| `下周一早上10点提醒我交报告` | 下周一 10:00 弹通知 |
| `晚上7点提醒我去健身房` | 今天 19:00 弹通知（已过时间会警告） |

## 📁 项目结构

```
windows-reminder/
├── SKILL.md                          # Claude Code skill 定义
├── scripts/
│   ├── daemon.ps1                    # 后台轮询守护（60s 循环，单进程）
│   ├── show-notification.ps1         # 弹 toast + 更新 frontmatter 状态
│   ├── protocol-handler.ps1          # 处理 windows-reminder:// URL（完成按钮）
│   ├── protocol-launcher.vbs         # 隐藏 PowerShell 窗口
│   ├── register-protocol.ps1         # 注册自定义协议到注册表
│   └── startup-check.ps1             # 开机扫描过期提醒 + 启动守护 + 自注册
├── CHANGELOG.md                      # 版本历史
├── README.md                         # 英文版
├── README_CN.md                      # 中文版（本文件）
└── .gitignore
```

### Vault 侧（skill 运行时自动创建）

```
vault/
├── reminders/                        # 每提醒一个 .md 笔记
│   ├── 2026-06-03-lunch.md
│   └── 2026-06-04-meeting.md
└── 提醒记录.md                       # Dataview 汇总视图
```

## 📝 提醒笔记格式

```markdown
---
status: waiting
trigger: 2026-06-03 12:00
created: 2026-06-03 11:08
task_name: ClaudeReminder-lunch
---

# 吃饭提醒

`INPUT[inlineSelect(option(waiting, ⏳ 等待中), option(reminded, 🔔 已提醒),
  option(done, ✅ 已完成), option(pending, ❌ 待完成)):status]`

**触发时间**：2026年6月3日 12:00
**内容**：该吃饭了！
```

## 🔧 状态说明

| 状态 | 含义 | 谁改的 |
|------|------|--------|
| `waiting` | 等待触发 | 系统（创建时） |
| `reminded` | 已弹通知 | 系统（自动） |
| `done` | 已完成 | 手动（下拉栏） |
| `pending` | 待完成 | 手动（下拉栏） |

## ⚠️ 已知限制

- **PowerShell 5.1 编码**：`.ps1` 文件必须纯 ASCII 或带 BOM 的 UTF-8。Skill 内部已处理——定时脚本不含中文，`show-notification.ps1` 从笔记文件读取中文内容。
- **无持久化定时任务**：计时器是一次性后台进程。如果手动杀掉 PowerShell 进程，提醒会丢失，直到下次开机时 `startup-check.ps1` 补弹。
- **单机本地**：提醒是本地文件 + 本地进程，不支持多设备同步。

## 📄 许可证

MIT © ChenDianPeter-1

---

<p align="center">
  <sub>为 Obsidian + Claude Code 生态而建 ❤️</sub>
</p>
