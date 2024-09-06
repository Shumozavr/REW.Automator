using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shumozavr.Common.Messaging;
using Shumozavr.Common.SerialPorts;
using Shumozavr.RotatingTable.Common;

namespace Shumozavr.RotatingTable.Client;

public class RotatingTableClient : BaseRotatingTableDriver, IRotatingTableClient
{
    private readonly IOptionsMonitor<RotatingTableClientSettings> _settings;
    private Task? _rotatingTask;
    private double _currentAngle = 0;

    public RotatingTableClient(
        ILogger<RotatingTableClient> logger,
        IOptionsMonitor<RotatingTableClientSettings> settings,
        [FromKeyedServices(nameof(RotatingTableClient))]ISerialPort tablePort)
        : base(logger, tablePort)
    {
        _settings = settings;
    }

    public async Task<int> GetAcceleration(CancellationToken cancellationToken)
    {
        using var @lock = await AcquireCommandLock();
        using var subscription = await TablePort.Subscribe();

        TablePort.SendCommand("GET ACC");

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_settings.CurrentValue.CommandInitiationTimeout);
        await foreach (var token in subscription.MessagesReader.ReadAllAsync(cts.Token))
        {
            if (int.TryParse(token, CultureInfo.InvariantCulture, out var acceleration))
            {
                return acceleration;
            }
        }

        throw new InvalidOperationException("No acceleration value was received");
    }

    public async Task Reset(CancellationToken cancellationToken)
    {
        await Rotate(-_currentAngle, cancellationToken);
        _currentAngle = 0;
    }

    public async Task SetAcceleration(int acceleration, CancellationToken cancellationToken)
    {
        if (acceleration is < 1 or > 10)
        {
            throw new ArgumentException("Acceleration must be between 1 and 10 ", nameof(acceleration));
        }

        using var @lock = await AcquireCommandLock();
        using var subscription = await TablePort.Subscribe();
        TablePort.SendCommand($"SET ACC {acceleration}");
        await WaitForCommandInit(subscription, cancellationToken);
    }

    public async Task<IAsyncEnumerable<double>> StartRotating(double angle, CancellationToken cancellationToken)
    {
        if (Math.Abs(angle) < 0.0001)
        {
            return AsyncEnumerable.Empty<double>();
        }

        var subscription = await TablePort.Subscribe();
        try
        {
            using var @lock = await AcquireCommandLock();
            TablePort.SendCommand($"FM {angle}");
            await WaitForCommandInit(subscription, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to start rotating command");
            subscription.Dispose();
            throw;
        }

        var positions = Channel.CreateUnbounded<double>();
        _rotatingTask = Task.Run(
            async () =>
            {
                try
                {
                    await foreach (var position in ProcessPositions(subscription.MessagesReader, CancellationToken.None))
                    {
                        positions.Writer.TryWrite(position);
                    }

                    positions.Writer.Complete();
                }
                catch (Exception e)
                {
                    positions.Writer.TryComplete(e);
                    Logger.LogError(e, "Failed to finish rotating command");
                }
                finally
                {
                    subscription.Dispose();
                }
            },
            CancellationToken.None);

        return positions.Reader.ReadAllAsync(CancellationToken.None);

        async IAsyncEnumerable<double> ProcessPositions(ChannelReader<string> messages, [EnumeratorCancellation] CancellationToken ct)
        {
            var currentAngle = 0.0;
            await foreach (var token in messages.ReadAllAsync(ct))
            {
                switch (token)
                {
                    case var _ when token.StartsWith("POS"):
                        currentAngle = double.Parse(token["POS".Length..].Trim(), CultureInfo.InvariantCulture);
                        Logger.LogInformation("Table at position {X}", currentAngle);
                        yield return currentAngle;
                        break;
                    case "END":
                        _currentAngle += currentAngle;
                        Logger.LogInformation("Table finished rotating");
                        yield break;
                }
            }

            throw new InvalidOperationException("Rotating command must end with END token");
        }
    }

    public async Task<double?> Rotate(double angle, CancellationToken cancellationToken)
    {
        var positionsStream = await StartRotating(angle, cancellationToken);

        double? lastPos = null;
        try
        {
            await foreach (var pos in positionsStream.WithCancellation(cancellationToken))
            {
                lastPos = pos;
            }
        }
        catch (OperationCanceledException)
        {
            await Stop(softStop: true, CancellationToken.None);
            return lastPos;
        }

        return lastPos;
    }

    public async Task Stop(bool softStop, CancellationToken cancellationToken)
    {
        if (_rotatingTask == null || _rotatingTask.IsCompleted)
        {
            Logger.LogInformation("Table was not rotating, nothing to stop");
            return;
        }

        using var @lock = await AcquireCommandLock();
        using var subscription = await TablePort.Subscribe();
        TablePort.SendCommand(softStop ? "SOFTSTOP" : "STOP");
        await WaitForCommandInit(subscription, cancellationToken);

        if (_rotatingTask != null)
        {
            await _rotatingTask;
            Logger.LogInformation("Rotating stopped");
        }
    }

    private async Task WaitForCommandInit(Subscription<string> subscription, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_settings.CurrentValue.CommandInitiationTimeout);
        var message = await subscription.WaitForMessage(m => m is "OK" or "ERR", cts.Token);
        if (message is "OK")
        {
            Logger.LogInformation("Command started");
            return;
        }

        throw new InvalidOperationException("Failed to init command");
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await Stop(softStop: true, CancellationToken.None);
    }

    public sealed override async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}