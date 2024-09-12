using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shumozavr.Common.Messaging;

namespace Shumozavr.Common.SerialPorts;

public sealed class SerialPortWrapper : ISerialPort
{
    private readonly string _serviceKey;
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<SerialPortSettings> _settings;
    private readonly IEventBus<string> _tableMessagesBus;

    private SerialPort? _tablePort;
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    public SerialPortWrapper(
        string serviceKey,
        ILogger<SerialPortWrapper> logger,
        IOptionsMonitor<SerialPortSettings> settings,
        EventBusFactory eventBusFactory)
    {
        _serviceKey = serviceKey;
        _logger = logger;
        _settings = settings;
        _tableMessagesBus = eventBusFactory.Create<string>();
    }

    public async Task ReInit()
    {
        await Init();
    }

    public async Task SendCommand(string command)
    {
        await EnsureInitialized();
        _logger.LogInformation("Sending command: {command}", command);
        _tablePort.WriteLine(command);
    }

    public async Task<Subscription<string>> Subscribe()
    {
        await EnsureInitialized();
        return await _tableMessagesBus.Subscribe();
    }

    private bool _disposed = false;
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _logger.LogInformation("Disposing...");
        await DisposePort();

        await CastAndDispose(_commandLock);
        _logger.LogInformation("Disposed...");
        return;
    }

    private async Task DisposePort()
    {
        if (_tablePort == null)
        {
            return;
        }
        _tablePort.DataReceived -= OnDataReceived;
        _tablePort.ErrorReceived -= OnErrorReceived;
        _tablePort.Close();
        await CastAndDispose(_tablePort);
        _logger.LogInformation("Port Disposed...");
    }

    private static async ValueTask CastAndDispose(IDisposable? resource)
    {
        if (resource == default)
        {
            return;
        }
        if (resource is IAsyncDisposable resourceAsyncDisposable)
            await resourceAsyncDisposable.DisposeAsync();
        else
            resource.Dispose();
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs args)
    {
        _logger.LogError("Error received: {EventType}", args.EventType);

        _tableMessagesBus.TryComplete();
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
    {
        EnsureInitializedOrThrow();
        _logger.LogTrace("Data received: {EventType}", args.EventType);
        try
        {
            switch (args.EventType)
            {
                case SerialData.Chars:
                {
                    var message = _tablePort.ReadLine();

                    _logger.LogInformation("Message: {portMessage}", message);
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

    [MemberNotNull(nameof(_tablePort))]
    private async Task EnsureInitialized(CancellationToken cancellationToken = default)
    {
        if (_tablePort?.IsOpen != true)
        {
            await Init();
        }
    }

    [MemberNotNull(nameof(_tablePort))]
    private async Task Init()
    {
        _logger.LogInformation("Opening serial port...");
        await DisposePort();
        _tablePort = CreateSerialPort(_settings.Get(_serviceKey));
        for (var i = 0; i < _settings.CurrentValue.ReconnectRetryCount; i++)
        {
            await Task.Delay(_settings.CurrentValue.ReconnectDelay);
            try
            {
                _tablePort.Open();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed open a serial port. Retry attempt: {i}", i);
            }
        }

        _logger.LogInformation("Serial port opened");
    }

    [MemberNotNull(nameof(_tablePort))]
    private Task EnsureInitializedOrThrow(CancellationToken cancellationToken = default)
    {
        if (_tablePort?.IsOpen == true)
        {
            return Task.CompletedTask;
        }
        throw new InvalidOperationException("Serial port is not initialized");
    }

    private SerialPort CreateSerialPort(SerialPortSettings settings)
    {
        var port = new SerialPort(settings.PortName)
        {
            BaudRate = settings.BaudRate,
            ReadTimeout = SerialPort.InfiniteTimeout,
            WriteTimeout = (int)settings.WritePortTimeout.TotalMilliseconds,

        };
        port.DataReceived += OnDataReceived;
        port.ErrorReceived += OnErrorReceived;
        return port;
    }
}