using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shumozavr.Common;
using Shumozavr.Common.Messaging;
using Shumozavr.Common.SerialPorts;

namespace Shumozavr.RotatingTable.Common;

public abstract class BaseRotatingTableDriver : IAsyncDisposable
{
    protected readonly ILogger Logger;

    protected readonly ISerialPort TablePort;
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    protected BaseRotatingTableDriver(
        ILogger logger,
        ISerialPort serialPort)
    {
        TablePort = serialPort;
        Logger = logger;
    }

    protected virtual async Task<LockWrapper> AcquireCommandLock()
    {
        try
        {
            return await LockWrapper.LockOrThrow(_commandLock);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Unable to start multiple commands simultaneously");
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        await TablePort.DisposeAsync();
        _commandLock.Dispose();
    }
}