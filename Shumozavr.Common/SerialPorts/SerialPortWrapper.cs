using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shumozavr.Common.Messaging;

namespace Shumozavr.Common.SerialPorts;

public sealed class SerialPortWrapper : ISerialPort
{
    private readonly ILogger _logger;
    private readonly IEventBus<string> _tableMessagesBus;

    private readonly SerialPort _tablePort;
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    public SerialPortWrapper(
        string serviceKey,
        ILogger<SerialPortWrapper> logger,
        IOptionsMonitor<SerialPortSettings> settings,
        EventBusFactory eventBusFactory)
    {
        _tablePort = CreateSerialPort(settings.Get(serviceKey));
        _logger = logger;
        _tableMessagesBus = eventBusFactory.Create<string>();

        _tablePort.DataReceived += OnDataReceived;
        _tablePort.ErrorReceived += OnErrorReceived;
    }

    public void SendCommand(string command)
    {
        _tablePort.WriteLine(command);
    }

    public Task<Subscription<string>> Subscribe()
    {
        return _tableMessagesBus.Subscribe();
    }

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_tablePort);
        await CastAndDispose(_commandLock);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs args)
    {
        _logger.LogError("Error received: {EventType}", args.EventType);

        _tableMessagesBus.TryComplete();
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
    {
        _logger.LogTrace("Data received: {EventType}", args.EventType);
        try
        {
            switch (args.EventType)
            {
                case SerialData.Chars:
                {
                    var message = _tablePort.ReadLine();

                    _logger.LogTrace("Message: {portMessage}", message);
                    _tableMessagesBus.Publish(message);
                    break;
                }
                case SerialData.Eof:
                    _logger.LogInformation("Serial port received EOF");
                    _tableMessagesBus.TryComplete();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args.EventType), args.EventType, null);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong handling table port");
            _tableMessagesBus.TryComplete(e);
        }
    }

    private static SerialPort CreateSerialPort(SerialPortSettings settings)
    {
        var port = new SerialPort(settings.PortName)
        {
            BaudRate = 115200,
            ReadTimeout = (int)settings.ReadPortTimeout.TotalMilliseconds,
            WriteTimeout = (int)settings.WritePortTimeout.TotalMilliseconds,
        };
        port.Open();
        return port;
    }
}