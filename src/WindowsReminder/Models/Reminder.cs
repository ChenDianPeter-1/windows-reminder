using System.IO;

namespace WindowsReminder.Models;

public class Reminder
{
    public string FilePath { get; set; } = "";
    public string FileName => Path.GetFileName(FilePath);

    // Frontmatter fields
    public ReminderStatus Status { get; set; } = ReminderStatus.Unknown;
    public DateTime? Trigger { get; set; }
    public DateTime? Created { get; set; }
    public string TaskName { get; set; } = "";

    // Extracted from Markdown body
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";

    public bool ShouldNotify()
    {
        if (Status != ReminderStatus.Waiting) return false;
        if (Trigger == null) return false;
        return Trigger.Value <= DateTime.Now;
    }

    public override string ToString()
        => $"[{Status}] {Title} (trigger={Trigger:yyyy-MM-dd HH:mm}, task={TaskName})";
}
