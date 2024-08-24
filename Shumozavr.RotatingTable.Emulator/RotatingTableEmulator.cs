using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Shumozavr.Common;
using Shumozavr.RotatingTable.Common;

namespace Shumozavr.RotatingTable.Emulator;

public class RotatingTableEmulator : IDisposable
{
    private readonly SerialPort _tablePort;
    private readonly ILogger<RotatingTableEmulator> _logger;
    private int _acceleration = 1;
    private readonly SemaphoreSlim _commandLock = new(1, 1);
    private readonly CancellationTokenSource _rotatingCt;

    public RotatingTableEmulator(
        ILogger<RotatingTableEmulator> logger)
    {
        _tablePort = tablePort;
        _logger = logger;
        _rotatingCt = new CancellationTokenSource();

        _tablePort.DataReceived += OnDataReceived;
        _tablePort.ErrorReceived += OnErrorReceived;
    }
    
    protected virtual void OnErrorReceived(object sender, SerialErrorReceivedEventArgs args)
    {
        _logger.LogError("Error received: {EventType}", args.EventType);
    }

    protected virtual async void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
    {
        _logger.LogTrace("Data received: {EventType}", args.EventType);
        try
        {
            switch (args.EventType)
            {
                case SerialData.Chars:
                {
                    var message = _tablePort.ReadLine();
                    await Handle(message);
                    _logger.LogTrace("Message: {portMessage}", message);

                    break;
                }
                case SerialData.Eof:
                    _logger.LogInformation("Serial port received EOF");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args.EventType), args.EventType, null);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong handling table port");
        }
    }

    private async Task Handle(string token)
    {
        switch (token)
        {
            case "GET ACC":
                using (await AcquireCommandLock())
                {
                    _tablePort.WriteLine(_acceleration.ToString());
                }

                break;
            case not null when token.StartsWith("SET ACC") && int.TryParse(token["SET ACC".Length..], out var acceleration):
                using (await AcquireCommandLock())
                {
                    _acceleration = acceleration;
                    _tablePort.WriteLine("OK");
                }

                break;
            case not null when token.StartsWith("FM") && int.TryParse(token["FM".Length..], out var desiredAngle):
                using (await AcquireCommandLock())
                {
                    _tablePort.WriteLine("OK");
                }

                Task.Run(async () =>
                {
                    try
                    {
                        for (var currentAngle = 0; currentAngle < desiredAngle; currentAngle += desiredAngle / 5)
                        {
                            if (_rotatingCt.IsCancellationRequested)
                            {
                                _rotatingCt.TryReset();
                                break;
                            }

                            _tablePort.WriteLine($"POS {currentAngle}");
                            await Task.Delay(300);
                        }

                        _tablePort.WriteLine("END");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Something went wrong handling rotating command");
                        _tablePort.WriteLine("END");
                    }
                });
                break;
            case "SOFTSTOP":
            case "STOP":
                using (await AcquireCommandLock())
                {
                    _rotatingCt.Cancel();
                    _tablePort.WriteLine("OK");
                }

                break;
        }
    }

    public void Dispose()
    {
        _tablePort.DataReceived -= OnDataReceived;
        _tablePort.ErrorReceived -= OnErrorReceived;
        _tablePort.Dispose();
    }

    protected async Task<LockWrapper> AcquireCommandLock()
    {
        try
        {
            return await LockWrapper.LockOrThrow(_commandLock);
        }
        catch (Exception e)
        {
            _tablePort.WriteLine("ERR");
            throw new InvalidOperationException("Unable to start multiple commands simultaneously");
        }
    }
}