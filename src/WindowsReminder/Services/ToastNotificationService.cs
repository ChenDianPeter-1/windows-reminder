using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;

namespace WindowsReminder.Services;

/// <summary>
/// Toast notification service. All file references in toast args use short file names only
/// (args length limit in Windows toasts). Handlers resolve full paths via RemindersPath.
/// </summary>
public class ToastNotificationService
{
    private readonly ILogger<ToastNotificationService> _logger;

    public event EventHandler<string>? DoneClicked;
    public event EventHandler<SnoozeEventArgs>? SnoozeClicked;
    public event EventHandler<string>? OpenNoteClicked;

    public ToastNotificationService(ILogger<ToastNotificationService> logger)
    {
        _logger = logger;
        ToastNotificationManagerCompat.OnActivated += HandleActivation;
    }

    public void SendTestToast()
    {
        new ToastContentBuilder()
            .AddText("Windows Reminder")
            .AddText("This is a test notification")
            .AddButton("Done", ToastActivationType.Foreground, "action=done")
            .Show(toast => toast.ExpirationTime = DateTime.Now.AddMinutes(2));
        _logger.LogInformation("Test toast sent");
    }

    public void SendReminderToast(string title, string message, string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .AddButton("Done", ToastActivationType.Foreground, $"action=done&file={fileName}")
            .AddButton("Snooze 5m", ToastActivationType.Foreground, $"action=snooze&minutes=5&file={fileName}")
            .AddButton("Snooze 15m", ToastActivationType.Foreground, $"action=snooze&minutes=15&file={fileName}")
            .AddButton("Open Note", ToastActivationType.Foreground, $"action=opennote&file={fileName}")
            .Show(toast => toast.ExpirationTime = DateTime.Now.AddMinutes(10));
        _logger.LogInformation("Reminder toast sent: {Title} ({File})", title, fileName);
    }

    private void HandleActivation(ToastNotificationActivatedEventArgsCompat args)
    {
        var a = args.Argument ?? "";
        _logger.LogInformation("Toast activated: {Args}", a);

        var file = ExtractParam(a, "file");
        if (string.IsNullOrEmpty(file) || !IsSafeFileName(file))
        {
            _logger.LogWarning("Toast activation — missing or unsafe file param: {File}", file ?? "(null)");
            return;
        }

        if (a.Contains("action=done"))
        {
            _logger.LogInformation("-> Done: {File}", file);
            DoneClicked?.Invoke(this, file);
        }
        else if (a.Contains("action=snooze"))
        {
            var minStr = ExtractParam(a, "minutes");
            if (int.TryParse(minStr, out var minutes))
            {
                _logger.LogInformation("-> Snooze {Min}m: {File}", minutes, file);
                SnoozeClicked?.Invoke(this, new SnoozeEventArgs(file, minutes));
            }
        }
        else if (a.Contains("action=opennote"))
        {
            _logger.LogInformation("-> Open Note: {File}", file);
            OpenNoteClicked?.Invoke(this, file);
        }
    }

    private static string ExtractParam(string args, string key)
    {
        var prefix = $"{key}=";
        var idx = args.IndexOf(prefix);
        if (idx < 0) return "";
        var start = idx + prefix.Length;
        var end = args.IndexOf('&', start);
        return end < 0 ? args.Substring(start) : args.Substring(start, end - start);
    }

    /// <summary>Reject path traversal attempts.</summary>
    private static bool IsSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Contains("..")) return false;
        if (name.Contains("/") || name.Contains("\\")) return false;
        return name == Path.GetFileName(name);
    }
}

public class SnoozeEventArgs : EventArgs
{
    public string FileName { get; }
    public int Minutes { get; }
    public SnoozeEventArgs(string fileName, int minutes) { FileName = fileName; Minutes = minutes; }
}
