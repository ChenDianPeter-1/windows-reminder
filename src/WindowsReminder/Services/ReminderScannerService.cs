using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using WindowsReminder.Models;

namespace WindowsReminder.Services;

public class ReminderScannerService
{
    private readonly ILogger<ReminderScannerService> _logger;
    private readonly ReminderFileService _fileService;
    private readonly ToastNotificationService _toastService;
    private readonly string _remindersPath;
    private readonly string _vaultRoot;
    private readonly string _obsidianVaultName;
    private readonly bool _dryRun;
    private readonly int _pollIntervalSeconds;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private readonly ConcurrentDictionary<string, DateTime> _recentlyFired = new();

    public bool IsRunning { get; private set; }
    public bool IsPaused { get; private set; }
    public DateTime? LastScanTime { get; private set; }
    public int LastTriggeredCount { get; private set; }
    public int TotalFired { get; private set; }

    public ReminderScannerService(
        ILogger<ReminderScannerService> logger,
        ReminderFileService fileService,
        ToastNotificationService toastService,
        string remindersPath,
        string vaultRoot,
        string obsidianVaultName,
        bool dryRun,
        int pollIntervalSeconds = 30)
    {
        _logger = logger;
        _fileService = fileService;
        _toastService = toastService;
        _remindersPath = remindersPath;
        _vaultRoot = vaultRoot;
        _obsidianVaultName = obsidianVaultName;
        _dryRun = dryRun;
        _pollIntervalSeconds = pollIntervalSeconds;

        _toastService.DoneClicked += HandleDone;
        _toastService.SnoozeClicked += HandleSnooze;
        _toastService.OpenNoteClicked += HandleOpenNote;
    }

    public void Start()
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        IsRunning = true;
        _logger.LogInformation("Scanner started. Path={Path} Interval={Interval}s DryRun={DryRun}",
            _remindersPath, _pollIntervalSeconds, _dryRun);

        _loopTask = Task.Run(() => RunLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        IsRunning = false;
        _logger.LogInformation("Scanner stopped. Total={Count}", TotalFired);
    }

    public void Pause() { IsPaused = true; _logger.LogInformation("Scanner paused"); }
    public void Resume() { IsPaused = false; _logger.LogInformation("Scanner resumed"); }
    public void ScanNow() { try { DoScan(); } catch (Exception ex) { _logger.LogError(ex, "Manual scan failed"); } }

    private async Task RunLoop(CancellationToken ct)
    {
        try { DoScan(); } catch (Exception ex) { _logger.LogError(ex, "Initial scan failed"); }
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_pollIntervalSeconds * 1000, ct);
            if (IsPaused) continue;
            try { DoScan(); } catch (Exception ex) { _logger.LogError(ex, "Scan cycle failed"); }
        }
    }

    private void DoScan()
    {
        LastScanTime = DateTime.Now;
        var now = DateTime.Now;
        var triggered = 0;

        if (!Directory.Exists(_remindersPath)) { return; }

        var files = Directory.GetFiles(_remindersPath, "*.md");
        foreach (var file in files)
        {
            try
            {
                var r = _fileService.ParseFile(file);
                if (r == null) continue;
                if (r.Status != ReminderStatus.Waiting && r.Status != ReminderStatus.Pending) continue;
                if (r.Trigger == null || r.Trigger.Value > now) continue;
                if (_recentlyFired.TryGetValue(file, out var last) && (now - last).TotalSeconds < 60) continue;

                _logger.LogInformation("Due: {Title} ({Trigger:HH:mm})", r.Title, r.Trigger);
                _toastService.SendReminderToast(r.Title, r.Content, file);

                if (!_dryRun && _fileService.WriteStatus(file, ReminderStatus.Reminded))
                { triggered++; TotalFired++; _recentlyFired[file] = now; }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error: {File}", Path.GetFileName(file)); }
        }

        LastTriggeredCount = triggered;
        if (triggered > 0)
            _logger.LogInformation("Scan done: {Count} fired", triggered);
    }

    // ── Callbacks ──

    private void HandleDone(object? sender, string fileName)
    {
        var path = SafeResolve(fileName);
        if (path == null) return;
        var r = _fileService.ParseFile(path);
        if (r == null) return;
        if (r.Status != ReminderStatus.Waiting && r.Status != ReminderStatus.Reminded && r.Status != ReminderStatus.Snoozed && r.Status != ReminderStatus.Pending)
        { _logger.LogWarning("Done: status {Status} not eligible ({File})", r.Status, fileName); return; }

        if (_dryRun) { _logger.LogInformation("[DryRun] Would mark done: {File}", fileName); return; }
        _fileService.WriteStatus(path, ReminderStatus.Done);
        _logger.LogInformation("Done -> done: {File}", fileName);
    }

    private void HandleSnooze(object? sender, SnoozeEventArgs e)
    {
        var path = SafeResolve(e.FileName);
        if (path == null) return;

        var newTrigger = DateTime.Now.AddMinutes(e.Minutes);

        if (_dryRun)
        {
            _logger.LogInformation("[DryRun] Would snooze {Min}m: {File} -> trigger={Trigger}",
                e.Minutes, e.FileName, newTrigger.ToString("HH:mm"));
            return;
        }

        var ok1 = _fileService.WriteTrigger(path, newTrigger);
        var ok2 = _fileService.WriteStatus(path, ReminderStatus.Waiting);
        if (ok1 && ok2)
            _logger.LogInformation("Snoozed {Min}m: {File} -> trigger={Trigger}", e.Minutes, e.FileName, newTrigger.ToString("HH:mm"));
        else
            _logger.LogWarning("Snooze write failed: {File}", e.FileName);
    }

    private void HandleOpenNote(object? sender, string fileName)
    {
        var path = SafeResolve(fileName);
        if (path == null) return;

        try
        {
            // Compute relative path from vault root
            var relPath = Path.GetRelativePath(_vaultRoot, path).Replace('\\', '/');
            var uri = $"obsidian://open?vault={Uri.EscapeDataString(_obsidianVaultName)}&file={Uri.EscapeDataString(relPath)}";
            _logger.LogInformation("Open Note: {Uri}", uri);
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open Note failed. Fallback: opening file directly");
            try { Process.Start("explorer.exe", $"/select,\"{path}\""); } catch { }
        }
    }

    /// <summary>Resolves a safe file name to full path. Rejects path traversal.</summary>
    private string? SafeResolve(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        var safe = Path.GetFileName(fileName);
        if (safe != fileName) { _logger.LogWarning("Path traversal rejected: {Name}", fileName); return null; }
        var full = Path.Combine(_remindersPath, safe);
        var resolvedDir = Path.GetDirectoryName(Path.GetFullPath(full)) ?? "";
        var baseDir = Path.GetFullPath(_remindersPath).TrimEnd(Path.DirectorySeparatorChar);
        if (!resolvedDir.Equals(baseDir, StringComparison.OrdinalIgnoreCase))
        { _logger.LogWarning("Path traversal rejected: {Name}", fileName); return null; }
        if (!File.Exists(full)) { _logger.LogWarning("File not found: {Name}", fileName); return null; }
        return full;
    }
}
