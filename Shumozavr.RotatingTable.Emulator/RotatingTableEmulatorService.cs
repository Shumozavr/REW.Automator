using Microsoft.Extensions.Hosting;

namespace Shumozavr.RotatingTable.Emulator;

public class RotatingTableEmulatorService : IHostedService
{
    private readonly RotatingTableEmulator _emulator;

    // сервис чисто резолвит эмулятор и все
    public RotatingTableEmulatorService(RotatingTableEmulator emulator)
    {
        _emulator = emulator;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}