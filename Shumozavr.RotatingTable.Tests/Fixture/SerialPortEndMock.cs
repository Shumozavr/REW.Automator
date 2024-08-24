using Shumozavr.Common.Messaging;
using Shumozavr.Common.SerialPorts;

namespace Shumozavr.RotatingTable.Tests.Fixture;

public sealed class SerialPortEndMock : ISerialPort
{
    private readonly IEventBus<string> _firstEndBus;
    private readonly IEventBus<string> _secondEndBus;

    public SerialPortEndMock(IEventBus<string> firstEndBus, IEventBus<string> secondEndBus)
    {
        _firstEndBus = firstEndBus;
        _secondEndBus = secondEndBus;
    }

    public void SendCommand(string command)
    {
        _firstEndBus.Publish(command);
    }

    public Task<Subscription<string>> Subscribe()
    {
        return _secondEndBus.Subscribe();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}