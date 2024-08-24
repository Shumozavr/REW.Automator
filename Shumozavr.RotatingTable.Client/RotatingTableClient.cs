using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shumozavr.Common.Messaging;
using Shumozavr.RotatingTable.Common;

namespace Shumozavr.RotatingTable.Client;

public class RotatingTableClient : BaseRotatingTableDriver, IRotatingTableClient
{
    public RotatingTableClient(
        ILogger<RotatingTableClient> logger,
        IOptionsMonitor<RotatingTableSettings> settings,
        [FromKeyedServices(nameof(RotatingTableClient))]
        IEventBus<string> tableMessagesBus)
        : base(logger, settings, tableMessagesBus)
    {
    }

    public async Task<int> GetAcceleration(CancellationToken cancellationToken)
    {
        using var @lock = await AcquireCommandLock();
        using var subscription = await TableMessagesBus.Subscribe();

        SendCommand("GET ACC");

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Settings.CurrentValue.CommandInitiationTimeout);
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

        using var @lock = await AcquireCommandLock();
        using var subscription = await TableMessagesBus.Subscribe();
        SendCommand($"SET ACC {acceleration}");
        await WaitForCommandInit(subscription, cancellationToken);
    }

    public async Task<IAsyncEnumerable<double>> StartRotating(double angle, CancellationToken cancellationToken)
    {
        using var @lock = await AcquireCommandLock();
        var subscription = await TableMessagesBus.Subscribe();
        try
        {
            SendCommand($"FM {angle}");
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
                        var currentAngle = long.Parse(token["POS".Length..].Trim());
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
        await foreach (var _ in (await StartRotating(angle, cancellationToken)).WithCancellation(cancellationToken))
        {
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        using var @lock = await AcquireCommandLock();
        using var subscription = await TableMessagesBus.Subscribe();
        SendCommand("STOP");
        await WaitForCommandInit(subscription, cancellationToken);
    }

    public async Task SoftStop(CancellationToken cancellationToken)
    {
        using var @lock = await AcquireCommandLock();
        using var subscription = await TableMessagesBus.Subscribe();
        SendCommand("SOFTSTOP");
        await WaitForCommandInit(subscription, cancellationToken);
    }
}