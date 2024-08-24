using Shumozavr.Common.Messaging;
using Shumozavr.Common.SerialPorts;

namespace Shumozavr.RotatingTable.Tests.Fixture;

public sealed class SerialPortMock
{
    public ISerialPort FirstEnd { get; set; }

    public ISerialPort SecondEnd { get; set; }

    public SerialPortMock(EventBusFactory eventBusFactory)
    {
        var firstEndBus = eventBusFactory.Create<string>();
        var secondEndBus = eventBusFactory.Create<string>();
        FirstEnd = new SerialPortEndMock(firstEndBus, secondEndBus);
        SecondEnd = new SerialPortEndMock(secondEndBus, firstEndBus);
    }
}