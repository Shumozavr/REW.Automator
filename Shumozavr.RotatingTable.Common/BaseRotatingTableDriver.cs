using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shumozavr.Common;
using Shumozavr.Common.Messaging;

namespace Shumozavr.RotatingTable.Common;

public abstract class BaseRotatingTableDriver {

    protected readonly ILogger Logger;
    protected readonly IOptionsMonitor<RotatingTableSettings> Settings;
    protected readonly IEventBus<string> TableMessagesBus;

    private readonly SerialPort _tablePort;
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    protected BaseRotatingTableDriver(
        ILogger logger,
        IOptionsMonitor<RotatingTableSettings> settings,
        IEventBus<string> tableMessagesBus)
    {
        _tablePort = CreateSerialPort(settings);
        Logger = logger;
        Settings = settings;
        TableMessagesBus = tableMessagesBus;

        _tablePort.DataReceived += OnDataReceived;
        _tablePort.ErrorReceived += OnErrorReceived;
    }

    public void Dispose()
    {
        _tablePort.DataReceived -= OnDataReceived;
        _tablePort.ErrorReceived -= OnErrorReceived;
        _tablePort.Dispose();
    }

    protected void SendCommand(string command)
    {
        _tablePort.WriteLine(command);
    }

    protected virtual async Task WaitForCommandInit(Subscription<string> subscription, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Settings.CurrentValue.CommandInitiationTimeout);
        await foreach (var token in subscription.MessagesReader.ReadAllAsync(cts.Token))
        {
            switch (token)
            {
                case "OK" or "ERR":
                    Logger.LogInformation("Command started");
                    return;
            }
        }

        throw new InvalidOperationException("Command was not started");
    }

    protected async Task<LockWrapper> AcquireCommandLock()
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

    protected virtual void OnErrorReceived(object sender, SerialErrorReceivedEventArgs args)
    {
        Logger.LogError("Error received: {EventType}", args.EventType);

        TableMessagesBus.TryComplete();
    }

    protected virtual void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
    {
        Logger.LogTrace("Data received: {EventType}", args.EventType);
        try
        {
            switch (args.EventType)
            {
                case SerialData.Chars:
                {
                    var message = _tablePort.ReadLine();

                    Logger.LogTrace("Message: {portMessage}", message);
                    TableMessagesBus.Publish(message);
                    break;
                }
                case SerialData.Eof:
                    Logger.LogInformation("Serial port received EOF");
                    TableMessagesBus.TryComplete();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args.EventType), args.EventType, null);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Something went wrong handling table port");
            TableMessagesBus.TryComplete(e);
        }
    }

    private static SerialPort CreateSerialPort(IOptionsMonitor<RotatingTableSettings> settings)
    {
        var setting = settings.CurrentValue;
        var port = new SerialPort(setting.SerialPort)
        {
            BaudRate = 115200,
            ReadTimeout = (int)setting.ReadPortTimeout.TotalMilliseconds,
            WriteTimeout = (int)setting.WritePortTimeout.TotalMilliseconds,
        };
        port.Open();
        return port;
    }
}