using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shumozavr.REW.RotatingTableClient;

public class RotatingTableClient : IRotatingTableClient
{
    private readonly SerialPort _tablePort;
    private readonly ILogger<RotatingTableClient> _logger;
    private readonly IOptionsMonitor<RotatingTableClientSettings> _settings;
    private readonly List<Channel<string>> _subscriptions = [];
    private readonly object _commandLock = new();

    public RotatingTableClient(SerialPort tablePort, ILogger<RotatingTableClient> logger, IOptionsMonitor<RotatingTableClientSettings> settings)
    {
        _tablePort = tablePort;
        _logger = logger;
        _settings = settings;

        _tablePort.DataReceived += OnDataReceived;
        _tablePort.ErrorReceived += OnErrorReceived;
    }

    public async Task<int> GetAcceleration(CancellationToken cancellationToken)
    {
        using var @lock = AcquireCommandLock();
        using var subscription = SubscribeForTableEvents();

        _tablePort.WriteLine("GET ACC");

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_settings.CurrentValue.CommandInitiationTimeout);
        await foreach (var token in subscription.MessagesReader.ReadAllAsync(cts.Token))
        {
            if (int.TryParse(token, out var acceleration))
            {
                return acceleration;
            }
        }

        throw new InvalidOperationException("No acceleration value was received");
    }

    public async Task SetAcceleration(int acceleration, CancellationToken cancellationToken)
    {
        if (acceleration is < 1 or > 10)
        {
            throw new ArgumentException("Acceleration must be between 1 and 10 ", nameof(acceleration));
        }

        using var @lock = AcquireCommandLock();
        using var subscription = SubscribeForTableEvents();
        _tablePort.WriteLine($"SET ACC {acceleration}");
        await WaitForCommandInit(subscription, cancellationToken);
    }

    public async Task<IAsyncEnumerable<double>> StartRotating(double angle, CancellationToken cancellationToken)
    {
        using var @lock = AcquireCommandLock();
        var subscription = SubscribeForTableEvents();
        try
        {
            _tablePort.WriteLine($"FM {angle}");
            await WaitForCommandInit(subscription, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start rotating command");
            subscription.Dispose();
            throw;
        }
        var positions = Channel.CreateUnbounded<double>();
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await foreach (var position in ProcessPositions(subscription.MessagesReader, cancellationToken))
                    {
                        positions.Writer.TryWrite(position);
                    }

                    positions.Writer.Complete();
                }
                catch (Exception e)
                {
                    positions.Writer.TryComplete(e);
                    _logger.LogError(e, "Failed to finish rotating command");
                }
                finally
                {
                    subscription.Dispose();
                }
            },
            cancellationToken);

        return positions.Reader.ReadAllAsync(cancellationToken);

        async IAsyncEnumerable<double> ProcessPositions(ChannelReader<string> messages, [EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var token in messages.ReadAllAsync(ct))
            {
                switch (token)
                {
                    case var _ when token.StartsWith("POS"):
                        var currentAngle = long.Parse(token["POS".Length..].Trim());
                        _logger.LogInformation("Table at position {X}", currentAngle);
                        yield return currentAngle;
                        break;
                    case "END":
                        _logger.LogInformation("Table finished rotating");
                        yield break;
                }
            }

            throw new InvalidOperationException("Rotating command must end with END token");
        }
    }

    public async Task Rotate(double angle, CancellationToken cancellationToken)
    {
        await foreach (var _ in (await StartRotating(angle, cancellationToken)).WithCancellation(cancellationToken))
        {
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        using var @lock = AcquireCommandLock();
        using var subscription = SubscribeForTableEvents();
        _tablePort.WriteLine("STOP");
        await WaitForCommandInit(subscription, cancellationToken);
    }

    public async Task SoftStop(CancellationToken cancellationToken)
    {
        using var @lock = AcquireCommandLock();
        using var subscription = SubscribeForTableEvents();
        _tablePort.WriteLine("SOFTSTOP");
        await WaitForCommandInit(subscription, cancellationToken);
    }

    public void Dispose()
    {
        _tablePort.Dispose();
    }

    private async Task WaitForCommandInit(Subscription<string> subscription, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_settings.CurrentValue.CommandInitiationTimeout);
        await foreach (var token in subscription.MessagesReader.ReadAllAsync(cts.Token))
        {
            switch (token)
            {
                case "OK" or "ERR":
                    _logger.LogInformation("Command started");
                    return;
            }
        }

        throw new InvalidOperationException("Command was not started");
    }

    private CommandLock AcquireCommandLock()
    {
        if (!Monitor.TryEnter(_commandLock))
        {
            throw new InvalidOperationException("Unable to start multiple commands simultaneously");
        }

        return new CommandLock(_commandLock);
    }

    private Subscription<string> SubscribeForTableEvents()
    {
        var channel = Channel.CreateUnbounded<string>();
        var subscription = new Subscription<string>(
            channel.Reader,
            disposeAction: () => {
                _subscriptions.Remove(channel);
                channel.Writer.TryComplete();
            });

        _subscriptions.Add(channel);

        return subscription;
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs args)
    {
        _logger.LogError("Error received: {EventType}", args.EventType);

        foreach (var subscription in _subscriptions)
        {
            if (!subscription.Writer.TryComplete())
            {
                _logger.LogWarning("Writer was already completed");
            }
        }
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
                    foreach (var subscription in _subscriptions)
                    {
                        subscription.Writer.TryWrite(message);
                    }
                    break;
                }
                case SerialData.Eof:
                    _logger.LogInformation("Table finished working");
                    foreach (var subscription in _subscriptions)
                    {
                        subscription.Writer.TryComplete();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args.EventType), args.EventType, null);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong handling table port");
            foreach (var subscription in _subscriptions)
            {
                subscription.Writer.TryComplete(e);
            }
        }
    }
}