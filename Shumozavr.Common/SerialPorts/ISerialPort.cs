using Shumozavr.Common.Messaging;

namespace Shumozavr.Common.SerialPorts;

public interface ISerialPort : IAsyncDisposable
{
    public void SendCommand(string command);
    public Task<Subscription<string>> Subscribe();
}