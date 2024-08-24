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
        using var @lock = await AcquireCommandLock();
        var subscription = await TablePort.Subscribe();
        try
        {
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
                    Logger.LogError(e, "Failed to finish rotating command");
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
                        var currentAngle = double.Parse(token["POS".Length..].Trim(), CultureInfo.InvariantCulture);
                        Logger.LogInformation("Table at position {X}", currentAngle);
                        yield return currentAngle;
                        break;
                    case "END":
                        Logger.LogInformation("Table finished rotating");
                        yield break;
                }
            }

            throw new InvalidOperationException("Rotating command must end with END token");
        }
    }

    public async Task Rotate(double angle, CancellationToken cancellationToken)
    {
        if (Math.Abs(angle) < 0.0001)
        {
            return;
        }
        await foreach (var _ in (await StartRotating(angle, cancellationToken)).WithCancellation(cancellationToken))
        {
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        using var @lock = await AcquireCommandLock();
        using var subscription = await TablePort.Subscribe();
        TablePort.SendCommand("STOP");
        await WaitForCommandInit(subscription, cancellationToken);
    }

    public async Task SoftStop(CancellationToken cancellationToken)
    {
        using var @lock = await AcquireCommandLock();
        using var subscription = await TablePort.Subscribe();
        TablePort.SendCommand("SOFTSTOP");
        await WaitForCommandInit(subscription, cancellationToken);
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
                    Logger.LogInformation("Command started");
                    return;
            }
        }

        throw new InvalidOperationException("Command was not started");
    }
}