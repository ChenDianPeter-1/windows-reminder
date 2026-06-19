using Serilog;

namespace WindowsReminder.Services;

public class SingleInstanceService : IDisposable
{
    private Mutex? _mutex;
    private readonly string _mutexName;

    public bool IsFirstInstance { get; private set; }

    public SingleInstanceService(string appUserModelId)
    {
        _mutexName = $"Global\\{appUserModelId}.SingleInstance";
    }

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, _mutexName, out bool createdNew);
        IsFirstInstance = createdNew;

        if (createdNew)
        {
            Log.Information("Single instance acquired: {MutexName}", _mutexName);
        }
        else
        {
            Log.Warning("Second instance detected — exiting (mutex: {MutexName})", _mutexName);
        }

        return createdNew;
    }

    public void Dispose()
    {
        if (IsFirstInstance)
        {
            try { _mutex?.ReleaseMutex(); } catch { }
        }
        _mutex?.Dispose();
        Log.Information("Mutex released");
    }
}
