using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shumozavr.Common;
using Shumozavr.Common.SerialPorts;
using Shumozavr.RotatingTable.Common;

namespace Shumozavr.RotatingTable.Emulator;

public class RotatingTableEmulator : BaseRotatingTableDriver, IAsyncDisposable
{
    private readonly ILogger<RotatingTableEmulator> _logger;
    private readonly IOptionsMonitor<RotatingTableEmulatorSettings> _settings;
    private int _acceleration = 1;
    private readonly SemaphoreSlim _commandLock = new(1, 1);
    private CancellationTokenSource? _rotatingCt;
    private readonly Task _processTask;
    public Task? RotatingTask;
    public Func<int, double> GetRotatingStep { get; set; } = angleToRotate => angleToRotate / 5d;
    public Func<Task> RotatingDelay { get; set; } = () => Task.Delay(TimeSpan.FromMilliseconds(300));
    private readonly CancellationTokenSource _processCt;

    public RotatingTableEmulator(
        ILogger<RotatingTableEmulator> logger,
        IOptionsMonitor<RotatingTableEmulatorSettings> settings,
        [FromKeyedServices(nameof(RotatingTableEmulator))]ISerialPort serialPort) : base(logger, serialPort)
    {
        _logger = logger;
        _settings = settings;
        _processCt = new CancellationTokenSource();
        _processTask = Task.Run(
            async () =>
            {
                using var subscription = await TablePort.Subscribe();
                await foreach (var token in subscription.MessagesReader.ReadAllAsync(_processCt.Token))
                {
                    await Handle(token);
                }
            });
    }

    private async Task Handle(string token)
    {
        _logger.LogInformation("handling {token}", token);
        switch (token)
        {
            case "GET ACC":
                using (await AcquireCommandLock())
                {
                    TablePort.SendCommand(_acceleration.ToString());
                }

                break;
            case not null when token.StartsWith("SET ACC") && int.TryParse(token["SET ACC".Length..], CultureInfo.InvariantCulture, out var acceleration):
                using (await AcquireCommandLock())
                {
                    _acceleration = acceleration;
                    TablePort.SendCommand("OK");
                }

                break;
            case not null when token.StartsWith("FM") && int.TryParse(token["FM".Length..], CultureInfo.InvariantCulture, out var desiredAngle):
                using (await AcquireCommandLock())
                {
                    TablePort.SendCommand("OK");
                }

                if (RotatingTask is { IsCompleted: false })
                {
                    throw new InvalidOperationException("RotatingTask must be completed to proceed");
                }
                _rotatingCt?.Dispose();
                _rotatingCt = CancellationTokenSource.CreateLinkedTokenSource(_processCt.Token);
                RotatingTask = Task.Run(async () =>
                {
                    try
                    {
                        var angle = 0d;
                        while (angle < desiredAngle)
                        {
                            _logger.LogInformation("current angle {angle}", angle);
                            if (_rotatingCt.Token.IsCancellationRequested)
                            {
                                return;
                            }

                            var step = Math.Min(GetRotatingStep(desiredAngle), desiredAngle - angle);
                            TablePort.SendCommand($"POS {angle}");
                            await RotatingDelay();

                            angle += step;
                        }
                        TablePort.SendCommand($"POS {angle}");
                        _logger.LogInformation("current angle {angle}", angle);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Something went wrong handling rotating command");
                    }
                    finally
                    {
                        TablePort.SendCommand("END");
                    }
                });
                break;
            case "SOFTSTOP":
            case "STOP":
                using (await AcquireCommandLock())
                {
                    if (_rotatingCt == null)
                    {
                        throw new InvalidOperationException("Rotating CT must be initialized");
                    }
                    _rotatingCt.Cancel();
                    TablePort.SendCommand("OK");
                }

                break;
        }
    }

    protected override Task<LockWrapper> AcquireCommandLock()
    {
        try
        {
            return base.AcquireCommandLock();
        }
        catch
        {
            TablePort.SendCommand("ERR");
            throw;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        try
        {
            if (RotatingTask is { IsCompleted: false })
            {
                _rotatingCt?.Cancel();
                await RotatingTask;
            }
        }
        catch (OperationCanceledException)
        {
        }

        try
        {
            if (_processTask is { IsCompleted: false })
            {
                _processCt.Cancel();
                await _processTask;
            }
        }
        catch (OperationCanceledException)
        {
        }


        await CastAndDispose(_commandLock);
        await CastAndDispose(_rotatingCt);
        await CastAndDispose(RotatingTask);
        await CastAndDispose(_processTask);
        await CastAndDispose(_processCt);

        return;

        static async ValueTask CastAndDispose(IDisposable? resource)
        {
            switch (resource)
            {
                case null:
                    return;
                case IAsyncDisposable resourceAsyncDisposable:
                    await resourceAsyncDisposable.DisposeAsync();
                    break;
                default:
                    resource.Dispose();
                    break;
            }
        }
    }

    private bool _disposed = false;
    public sealed override async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        await DisposeAsyncCore();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}