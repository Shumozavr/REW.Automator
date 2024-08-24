using System.Runtime.CompilerServices;

namespace Shumozavr.Common;

public readonly struct LockWrapper : IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    private LockWrapper(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
    }

    public void Dispose()
    {
        _semaphore.Release();
    }

    public static async Task<LockWrapper> LockOrThrow(SemaphoreSlim semaphore, [CallerMemberName] string? callerMemberName = null)
    {
        if (!await semaphore.WaitAsync(0))
        {
            throw new InvalidOperationException($"Cannot acquire a lock in {callerMemberName}");
        }
        return new LockWrapper(semaphore);
    }
}