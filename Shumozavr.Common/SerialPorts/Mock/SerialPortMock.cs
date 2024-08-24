using Microsoft.Extensions.Logging;
using Shumozavr.Common.Messaging;

namespace Shumozavr.Common.SerialPorts.Mock;

public sealed class SerialPortMock
{
    public ISerialPort ClientEnd { get; set; }

    public ISerialPort ServerEnd { get; set; }

    public SerialPortMock(EventBusFactory eventBusFactory, ILoggerFactory loggerFactory)
    {
        var clientEndBus = eventBusFactory.Create<string>();
        var serverEndBus = eventBusFactory.Create<string>();
        ClientEnd = new SerialPortEndMock(clientEndBus, serverEndBus, loggerFactory.CreateLogger("Client " + nameof(SerialPortEndMock)));
        ServerEnd = new SerialPortEndMock(serverEndBus, clientEndBus, loggerFactory.CreateLogger("Server " + nameof(SerialPortEndMock)));
    }
}