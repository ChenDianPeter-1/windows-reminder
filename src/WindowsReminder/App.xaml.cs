using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using WindowsReminder.Models;
using WindowsReminder.Services;
using WindowsReminder.Tray;

namespace WindowsReminder;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private TrayIconManager? _tray;
    private SingleInstanceService? _singleInstance;
    private ReminderScannerService? _scanner;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var settings = config.Get<AppSettings>()!;
        var logDir = Environment.ExpandEnvironmentVariables(settings.LogDirectory);
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logDir, "windows-reminder-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                encoding: System.Text.Encoding.UTF8)
            .CreateLogger();

        _singleInstance = new SingleInstanceService(settings.AppUserModelId);
        if (!_singleInstance.TryAcquire())
        {
            Log.CloseAndFlush();
            Shutdown();
            return;
        }

        var remindersPath = Path.Combine(settings.VaultRoot, settings.RemindersRelativePath);

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((ctx, services) =>
            {
                services.AddSingleton(settings);
                services.AddSingleton<StartMenuShortcutService>();
                services.AddSingleton<ToastNotificationService>();
                services.AddSingleton<ReminderFileService>();
                services.AddSingleton<AutostartService>();
                services.AddSingleton(sp => new ReminderScannerService(
                    sp.GetRequiredService<ILogger<ReminderScannerService>>(),
                    sp.GetRequiredService<ReminderFileService>(),
                    sp.GetRequiredService<ToastNotificationService>(),
                    remindersPath,
                    settings.VaultRoot,
                    settings.ObsidianVaultName,
                    settings.DryRun,
                    settings.PollIntervalSeconds));
            })
            .Build();

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("App starting — reminders path: {Path}", remindersPath);

        var shortcutService = _host.Services.GetRequiredService<StartMenuShortcutService>();
        shortcutService.EnsureShortcutExists();

        var toastService = _host.Services.GetRequiredService<ToastNotificationService>();
        _scanner = _host.Services.GetRequiredService<ReminderScannerService>();
        if (settings.EnableScanner) _scanner.Start();

        var autostart = _host.Services.GetRequiredService<AutostartService>();
        if (settings.AutoStart) autostart.Enable();
        logger.LogInformation("AutoStart status: {Enabled}", autostart.IsEnabled);

        _tray = new TrayIconManager(
            _host.Services.GetRequiredService<ILogger<TrayIconManager>>(),
            settings.LogDirectory,
            onTestToast: () => toastService.SendTestToast(),
            onScanNow: () => _scanner.ScanNow(),
            onPause: () => _scanner.Pause(),
            onResume: () => _scanner.Resume(),
            onGetStatus: () =>
                $"Scanner: {(_scanner.IsPaused ? "PAUSED" : "RUNNING")}\n" +
                $"DryRun: {settings.DryRun}\n" +
                $"AutoStart: {(autostart.IsEnabled ? "ON" : "OFF")}\n" +
                $"Path: {remindersPath}\n" +
                $"Last Scan: {_scanner.LastScanTime?.ToString("HH:mm:ss") ?? "never"}\n" +
                $"Last Triggered: {_scanner.LastTriggeredCount}\n" +
                $"Total Fired: {_scanner.TotalFired}",
            onOpenRemindersFolder: () => Process.Start("explorer.exe", remindersPath),
            onEnableStartup: () => autostart.Enable(),
            onDisableStartup: () => autostart.Disable(),
            onGetStartupStatus: () => autostart.IsEnabled,
            onParseReminders: () => ParseAndLogReminders(settings, _host.Services.GetRequiredService<ReminderFileService>(), logger),
            onWriteBackTest: () => RunWriteBackTests(_host.Services.GetRequiredService<ReminderFileService>(), logger),
            onExit: () => ShutdownApp(logger));

        await Task.CompletedTask;
    }

    private static void ParseAndLogReminders(AppSettings settings, ReminderFileService service, ILogger<App> logger)
    {
        var path = Path.Combine(settings.VaultRoot, settings.RemindersRelativePath);
        if (!Directory.Exists(path)) { logger.LogWarning("Not found: {Path}", path); return; }

        var files = Directory.GetFiles(path, "*.md");
        logger.LogInformation("Scanning {Count} files in {Path}", files.Length, path);
        foreach (var f in files)
        {
            var r = service.ParseFile(f);
            if (r != null)
                logger.LogInformation("  {Status} | {Title} | {Trigger}", r.Status, r.Title, r.Trigger);
            else
                logger.LogWarning("  FAILED: {File}", Path.GetFileName(f));
        }
    }

    private static void RunWriteBackTests(ReminderFileService service, ILogger<App> logger)
    {
        var fixturesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\tests\Fixtures"));
        if (!Directory.Exists(fixturesDir)) { logger.LogWarning("Fixtures not found: {Path}", fixturesDir); return; }

        logger.LogInformation("=== WriteBack Tests ===");
        var testDir = Path.Combine(Path.GetTempPath(), $"wr-wb-{DateTime.Now:HHmmss}");
        Directory.CreateDirectory(testDir);
        foreach (var f in Directory.GetFiles(fixturesDir, "*.md"))
            File.Copy(f, Path.Combine(testDir, Path.GetFileName(f)), overwrite: true);

        var lunch = Path.Combine(testDir, "2026-06-03-lunch.md");
        if (File.Exists(lunch)) TestWB(service, logger, lunch, ReminderStatus.Reminded);
        File.Copy(Path.Combine(fixturesDir, "2026-06-03-lunch.md"), lunch, overwrite: true);
        if (File.Exists(lunch)) TestWB(service, logger, lunch, ReminderStatus.Done);
        var xzl = Path.Combine(testDir, "2026-06-03-xzl.md");
        if (File.Exists(xzl)) TestWB(service, logger, xzl, ReminderStatus.Done);
        try { Directory.Delete(testDir, recursive: true); } catch { }
    }

    private static void TestWB(ReminderFileService service, ILogger<App> logger, string path, ReminderStatus target)
    {
        var before = service.ParseFile(path);
        if (before == null) return;
        var ok = service.WriteStatus(path, target);
        var after = service.ParseFile(path);
        logger.LogInformation("  {Before} -> {Target}: OK={Ok}, Actual={Actual}",
            before.Status, target, ok, after?.Status);
    }

    private void ShutdownApp(ILogger<App> logger)
    {
        logger.LogInformation("App exiting");
        _scanner?.Stop();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        _host?.Dispose();
        Log.CloseAndFlush();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _scanner?.Stop();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        _host?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
