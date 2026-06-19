# Task 2: Reminder File Read, Markdown/YAML Parse, Status Write-back

## Date
2026-06-19

## Status: ✅ PASSED

All acceptance criteria met.

## Environment

| Item | Value |
|------|-------|
| YAML Parser | YamlDotNet 16.2.1 |
| Markdown Parsing | Regex-based (frontmatter split + H1 extraction) |
| Encoding | UTF-8 throughout (BOM handled gracefully) |

## New/Modified Files

### New
```
src/WindowsReminder/
├── Models/
│   ├── Reminder.cs               # Domain model: FilePath, Status, Trigger, Created, TaskName, Title, Content
│   └── ReminderStatus.cs         # Enum: Waiting/Reminded/Done/Pending/Snoozed
├── Services/
│   └── ReminderFileService.cs    # ParseFile(), ParseContent(), WriteStatus()
tests/
└── Fixtures/                     # Test copies of real reminder .md files
    ├── 2026-06-03-lunch.md
    ├── 2026-06-03-codex.md
    ├── 2026-06-03-xzl.md
    └── 2026-06-18-stop-vocab.md
```

### Modified
```
src/WindowsReminder/
├── WindowsReminder.csproj        # Added YamlDotNet 16.2.1
├── App.xaml.cs                   # Registered ReminderFileService, added ParseAndLogReminders + RunWriteBackTests
└── Tray/TrayIconManager.cs       # Added Debug > Parse Sample Reminders + Test WriteBack menu items
```

## Parsing Approach

### Frontmatter extraction
```
File content
  ├── Trim BOM ('﻿')
  ├── Find first "---" and second "---"
  ├── Between → YAML (YamlDotNet deserializes to ReminderFrontmatter)
  └── After → Markdown body
```

### YAML → Model mapping
| YAML key | Model property | Type |
|----------|---------------|------|
| status | Reminder.Status | enum (waiting/reminded/done/pending/snoozed) |
| trigger | Reminder.Trigger | DateTime? |
| created | Reminder.Created | DateTime? |
| task_name | Reminder.TaskName | string |

### Body extraction
| Field | Extraction rule |
|-------|----------------|
| Title | First `# Header` line |
| Content | Second `**...**：xxx` line (content field, after trigger field) |
| Fallback | Title = task_name, Content = Title |

## Write-back Approach

`WriteStatus()` uses `Regex.Replace` targeting only the first `status:` line within the frontmatter:

```csharp
var regex = new Regex(@"(?m)^status:\s*(.+)$");
var updated = regex.Replace(content, $"status: {StatusToString(newStatus)}", 1);
```

Before writing, the method validates that the body (everything after `---`) is unchanged:
```csharp
if (oldBody != newBody) { return false; }
```

All files are read and written with explicit UTF-8 (no BOM): `new UTF8Encoding(false)`.

## Edge Cases Handled

| Case | Handling |
|------|----------|
| BOM prefix (﻿) | `TrimStart('﻿')` before parsing |
| Duplicate frontmatter (stop-vocab.md) | Only first `---...---` block parsed; second one ignored |
| No H1 title | Falls back to task_name |
| No **内容** line | Falls back to title as content |
| Missing status field | `ParseStatus()` returns `ReminderStatus.Unknown`, ParseFile logs warning |
| Invalid trigger date | `DateTime.TryParse` — returns null on failure |

## Test Results

### Parse (16 real reminder files)
```
✅ All 16 files parsed successfully
✅ Statuses: 3 waiting, 11 reminded, 2 done
✅ Triggers: all extracted correctly
✅ Titles: 16/16 extracted (包括中文)
✅ Contents: 16/16 extracted
✅ Task names: 16/16 extracted
✅ Zero crashes
```

### Write-back (on fixture copies)
```
Test 1: waiting → reminded
  Before: [Waiting] 吃饭提醒
  After:  [Reminded] 吃饭提醒 | OK=True ✅

Test 2: waiting → done
  Before: [Waiting] 吃饭提醒
  After:  [Done] 吃饭提醒 | OK=True ✅

Test 3: reminded → done
  Before: [Reminded] 让 XZL 叫爸爸
  After:  [Done] 让 XZL 叫爸爸 | OK=True ✅
```

### Body integrity verification
All WriteStatus calls passed the internal body-integrity check (oldBody == newBody), confirming:
- inlineSelect line preserved
- Chinese content not corrupted
- Markdown formatting unchanged

## Acceptance Criteria Results

| # | Criterion | Result |
|---|-----------|--------|
| 1 | `dotnet build` succeeds | ✅ |
| 2 | Parse ≥3 real reminder .md files | ✅ (16 files parsed) |
| 3 | Extract status/trigger/created/task_name | ✅ |
| 4 | Extract title and content | ✅ |
| 5 | WriteStatus: waiting → reminded | ✅ |
| 6 | WriteStatus: waiting → done | ✅ |
| 7 | WriteStatus: reminded → done | ✅ |
| 8 | Body unchanged after write | ✅ |
| 9 | inlineSelect preserved | ✅ |
| 10 | Chinese not garbled | ✅ |
| 11 | No crash on malformed files | ✅ |

## Known Issues

1. **Serilog log encoding**: Chinese characters in log messages may appear garbled (e.g., "让 XZL 叫爸爸" → "�?XZL 叫爸�?"). This is a display issue only — file data is correctly UTF-8. Fix in future task: configure Serilog with `outputTemplate` and explicit UTF-8 encoding, or use Serilog.Sinks.Console without Chinese in log messages.

2. **Log message format**: The `ILogger` formatting produces verbose DateTime strings. Non-issue for development but should be cleaned up for production.

## Recommendation

**Proceed to Task 3** (ReminderScannerService — daemon polling + real toast firing).
