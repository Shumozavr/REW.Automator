namespace Shumozavr.REW.RotatingTableClient;

public struct CommandLock : IDisposable
{
    private readonly object _lockObject;

    public CommandLock(object lockObject)
    {
        _lockObject = lockObject;
    }

    public void Dispose()
    {
        Monitor.Exit(_lockObject);
    }
}