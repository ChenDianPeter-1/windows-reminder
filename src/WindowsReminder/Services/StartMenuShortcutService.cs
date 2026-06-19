using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace WindowsReminder.Services;

public class StartMenuShortcutService
{
    private readonly ILogger<StartMenuShortcutService> _logger;
    private readonly string _shortcutPath;

    public StartMenuShortcutService(ILogger<StartMenuShortcutService> logger)
    {
        _logger = logger;
        _shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Start Menu\Programs\WindowsReminder.lnk");
    }

    /// <summary>
    /// Ensures the Start Menu shortcut exists. Required for unpackaged app toast activation.
    /// Without this shortcut, toast button callbacks (OnActivated) will not fire.
    /// </summary>
    public void EnsureShortcutExists()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            _logger.LogError("Cannot determine exe path for shortcut");
            return;
        }

        if (File.Exists(_shortcutPath))
        {
            _logger.LogInformation("Start Menu shortcut exists: {Path}", _shortcutPath);
            return;
        }

        try
        {
            // Write a temp PowerShell script to create the shortcut
            // (avoids nested quoting issues with inline -Command)
            var tempScript = Path.Combine(Path.GetTempPath(), "wr-create-shortcut.ps1");
            var exeDir = Path.GetDirectoryName(exePath)!;
            var shortcutSafe = _shortcutPath.Replace("'", "''");
            var exePathSafe = exePath.Replace("'", "''");
            var exeDirSafe = exeDir.Replace("'", "''");
            var scriptContent =
                "$ws = New-Object -ComObject WScript.Shell\n" +
                "$sc = $ws.CreateShortcut('" + shortcutSafe + "')\n" +
                "$sc.TargetPath = '" + exePathSafe + "'\n" +
                "$sc.WorkingDirectory = '" + exeDirSafe + "'\n" +
                "$sc.Description = 'Windows Reminder'\n" +
                "$sc.Save()\n";
            // Write with UTF-8 BOM so PowerShell 5.1 doesn't garble it as GBK
            File.WriteAllText(tempScript, scriptContent, new System.Text.UTF8Encoding(true));

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            try { File.Delete(tempScript); } catch { /* ignore */ }

            if (File.Exists(_shortcutPath))
            {
                _logger.LogInformation("Start Menu shortcut created: {Path}", _shortcutPath);
            }
            else
            {
                var err = proc?.StandardError.ReadToEnd() ?? "";
                _logger.LogError("Failed to create shortcut: {Error}", err);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Start Menu shortcut");
        }
    }
}
