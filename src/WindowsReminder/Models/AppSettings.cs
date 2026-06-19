namespace WindowsReminder.Models;

public class AppSettings
{
    public string AppName { get; set; } = "Windows Reminder";
    public string AppUserModelId { get; set; } = "ChenDianPeter.WindowsReminder";
    public int PollIntervalSeconds { get; set; } = 60;
    public string VaultRoot { get; set; } = "";
    public string RemindersRelativePath { get; set; } = "reminders";
    public string LogDirectory { get; set; } = "%APPDATA%\\WindowsReminder\\logs";
    public string ObsidianVaultName { get; set; } = "Study";
    public bool EnableScanner { get; set; } = true;
    public bool DryRun { get; set; } = false;
    public bool AutoStart { get; set; } = true;
}
