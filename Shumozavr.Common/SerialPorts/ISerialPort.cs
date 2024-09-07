using Shumozavr.Common.Messaging;

namespace Shumozavr.Common.SerialPorts;

public interface ISerialPort : IAsyncDisposable
{
    public Task ReInit();
    public Task SendCommand(string command);
    public Task<Subscription<string>> Subscribe();
}