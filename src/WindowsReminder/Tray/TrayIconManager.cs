using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace WindowsReminder.Tray;

public class TrayIconManager : IDisposable
{
    private readonly ILogger<TrayIconManager> _logger;
    private readonly NotifyIcon _icon;
    private readonly string _logDirectory;

    public TrayIconManager(
        ILogger<TrayIconManager> logger,
        string logDirectory,
        Action onTestToast,
        Action onScanNow,
        Action onPause,
        Action onResume,
        Func<string> onGetStatus,
        Action onOpenRemindersFolder,
        Action onEnableStartup,
        Action onDisableStartup,
        Func<bool> onGetStartupStatus,
        Action onParseReminders,
        Action onWriteBackTest,
        Action onExit)
    {
        _logger = logger;
        _logDirectory = Environment.ExpandEnvironmentVariables(logDirectory);

        var menu = new ContextMenuStrip();

        // About
        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += (_, _) => ShowAbout();
        menu.Items.Add(aboutItem);
        menu.Items.Add(new ToolStripSeparator());

        // Test Toast
        var testToastItem = new ToolStripMenuItem("Test Toast");
        testToastItem.Click += (_, _) => { _logger.LogInformation("Test Toast"); onTestToast(); };
        menu.Items.Add(testToastItem);

        // Open Log
        var openLogItem = new ToolStripMenuItem("Open Log Folder");
        openLogItem.Click += (_, _) => { _logger.LogInformation("Open Log Folder"); OpenLogFolder(); };
        menu.Items.Add(openLogItem);
        menu.Items.Add(new ToolStripSeparator());

        // Open Reminders Folder
        var openRemindersItem = new ToolStripMenuItem("Open Reminders Folder");
        openRemindersItem.Click += (_, _) => { _logger.LogInformation("Open Reminders Folder"); onOpenRemindersFolder(); };
        menu.Items.Add(openRemindersItem);
        menu.Items.Add(new ToolStripSeparator());

        // Scanner controls
        var scannerMenu = new ToolStripMenuItem("Scanner");

        var scanNowItem = new ToolStripMenuItem("Scan Now");
        scanNowItem.Click += (_, _) => { _logger.LogInformation("Scan Now"); onScanNow(); };
        scannerMenu.DropDownItems.Add(scanNowItem);

        var statusItem = new ToolStripMenuItem("Show Scanner Status");
        statusItem.Click += (_, _) =>
        {
            _logger.LogInformation("Show Scanner Status");
            var status = onGetStatus();
            System.Windows.MessageBox.Show(status, "Scanner Status", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        scannerMenu.DropDownItems.Add(statusItem);

        scannerMenu.DropDownItems.Add(new ToolStripSeparator());

        var pauseItem = new ToolStripMenuItem("Pause Scanner");
        pauseItem.Click += (_, _) => { _logger.LogInformation("Pause Scanner"); onPause(); };
        scannerMenu.DropDownItems.Add(pauseItem);

        var resumeItem = new ToolStripMenuItem("Resume Scanner");
        resumeItem.Click += (_, _) => { _logger.LogInformation("Resume Scanner"); onResume(); };
        scannerMenu.DropDownItems.Add(resumeItem);

        menu.Items.Add(scannerMenu);
        menu.Items.Add(new ToolStripSeparator());

        // Startup controls
        var startupMenu = new ToolStripMenuItem("Startup");
        var enableItem = new ToolStripMenuItem("Enable Startup");
        enableItem.Click += (_, _) => { _logger.LogInformation("Enable Startup"); onEnableStartup(); };
        startupMenu.DropDownItems.Add(enableItem);
        var disableItem = new ToolStripMenuItem("Disable Startup");
        disableItem.Click += (_, _) => { _logger.LogInformation("Disable Startup"); onDisableStartup(); };
        startupMenu.DropDownItems.Add(disableItem);
        startupMenu.DropDownItems.Add(new ToolStripSeparator());
        var suStatusItem = new ToolStripMenuItem("Startup Status");
        suStatusItem.Click += (_, _) =>
        {
            var enabled = onGetStartupStatus();
            System.Windows.MessageBox.Show(
                $"Startup: {(enabled ? "ENABLED" : "DISABLED")}",
                "Startup Status", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        startupMenu.DropDownItems.Add(suStatusItem);
        menu.Items.Add(startupMenu);
        menu.Items.Add(new ToolStripSeparator());

        // Debug
        var debugMenu = new ToolStripMenuItem("Debug");
        var parseItem = new ToolStripMenuItem("Parse Sample Reminders");
        parseItem.Click += (_, _) => { _logger.LogInformation("Parse"); onParseReminders(); };
        debugMenu.DropDownItems.Add(parseItem);

        var wbItem = new ToolStripMenuItem("Test WriteBack (Fixtures)");
        wbItem.Click += (_, _) => { _logger.LogInformation("WriteBack"); onWriteBackTest(); };
        debugMenu.DropDownItems.Add(wbItem);
        menu.Items.Add(debugMenu);
        menu.Items.Add(new ToolStripSeparator());

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => { _logger.LogInformation("Exit"); onExit(); };
        menu.Items.Add(exitItem);

        _icon = new NotifyIcon
        {
            Text = "Windows Reminder",
            Icon = System.Drawing.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };

        _logger.LogInformation("Tray icon initialized");
    }

    private void ShowAbout()
    {
        System.Windows.MessageBox.Show(
            "Windows Reminder\nVersion 0.9.0-dev\n\n" +
            "Task 3 — Scanner + Toast + Done loop",
            "About Windows Reminder",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenLogFolder()
    {
        try
        {
            var dir = Directory.Exists(_logDirectory) ? _logDirectory : Path.GetDirectoryName(_logDirectory) ?? _logDirectory;
            Process.Start("explorer.exe", dir);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to open log folder"); }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _logger.LogInformation("Tray icon disposed");
    }
}
