namespace WindowsReminder.Models;

/// <summary>
/// All valid status values for a reminder. Mirrors the PowerShell version's frontmatter status field.
/// </summary>
public enum ReminderStatus
{
    Unknown = 0,
    Waiting = 1,    // Pending trigger (created, not yet fired)
    Reminded = 2,   // Toast fired, awaiting user action
    Done = 3,       // User marked complete (via Done button or manual dropdown)
    Pending = 4,    // Manual user status (rarely used)
    Snoozed = 5     // Snoozed (future compatibility)
}
