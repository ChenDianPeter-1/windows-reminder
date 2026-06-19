using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace WindowsReminder.Services;

public class AutostartService
{
    private readonly ILogger<AutostartService> _logger;
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WindowsReminder";

    public AutostartService(ILogger<AutostartService> logger)
    {
        _logger = logger;
    }

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            var value = key?.GetValue(ValueName) as string;
            return !string.IsNullOrEmpty(value) && value.Contains("WindowsReminder");
        }
    }

    public void Enable()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            _logger.LogError("Cannot determine exe path for AutoStart");
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key == null)
        {
            _logger.LogError("Cannot open Run key");
            return;
        }

        var quotedPath = $"\"{exePath}\"";
        key.SetValue(ValueName, quotedPath);
        _logger.LogInformation("AutoStart enabled: {Path}", quotedPath);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key == null) return;

        try { key.DeleteValue(ValueName, throwOnMissingValue: false); }
        catch { /* ignore */ }
        _logger.LogInformation("AutoStart disabled");
    }
}
