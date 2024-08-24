using Microsoft.Extensions.Logging;
using Shumozavr.Common.Messaging;

namespace Shumozavr.Common.SerialPorts.Mock;

public sealed class SerialPortEndMock : ISerialPort
{
    private readonly IEventBus<string> _clientEndBus;
    private readonly IEventBus<string> _serverEndBus;
    private readonly ILogger _logger;
    private readonly Task _loggingTask;
    private readonly CancellationTokenSource _loggingCt;

    public SerialPortEndMock(IEventBus<string> clientEndBus, IEventBus<string> serverEndBus, ILogger logger)
    {
        _clientEndBus = clientEndBus;
        _serverEndBus = serverEndBus;
        _logger = logger;
        _loggingCt = new CancellationTokenSource();
        _loggingTask = Task.Run(
            async () =>
            {
                using var bus = await _serverEndBus.Subscribe();
                await foreach (var token in bus.MessagesReader.ReadAllAsync(_loggingCt.Token))
                {
                    _logger.LogInformation("Received token: {Token}", token);
                }
            });
    }

    public void SendCommand(string command)
    {
        _logger.LogInformation("Sending command: {command}", command);
        _clientEndBus.Publish(command);
    }

    public Task<Subscription<string>> Subscribe()
    {
        _logger.LogTrace("subscribing...");
        return _serverEndBus.Subscribe();
    }

    private bool _disposed = false;
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _logger.LogTrace("disposeasync called");
        _loggingCt.Cancel();
        try
        {
            await _loggingTask;
        }
        catch (OperationCanceledException)
        {
        }

        _logger.LogTrace("disposing logging ct");
        _loggingCt.Dispose();
        _loggingTask.Dispose();
    }
}