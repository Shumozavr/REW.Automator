using System.Threading.Channels;
using Shumozavr.REW.Client.Http.Models;

namespace Shumozavr.REW.Client.Http;

public class RewMeasurementClient
{
    private readonly IRewMeasurementHttpClient _client;
    private readonly Channel<RewMessage> _rewMessages;

    public RewMeasurementClient(IRewMeasurementHttpClient client)
    {
        _client = client;
        _rewMessages = Channel.CreateUnbounded<RewMessage>();
    }

    public async Task SubscribeOnEvents(Uri callbackUri, CancellationToken cancellationToken)
    {
        await _client.SubscribeOnEvents(new SubscribeRequest(callbackUri.ToString()), cancellationToken);
    }

    public async Task Callback(RewMessage message, CancellationToken cancellationToken)
    {
        await _rewMessages.Writer.WriteAsync(message, cancellationToken);
    }

    public async Task SetOffsetTimeAtIRStart(string id, CancellationToken cancellationToken)
    {
        var info = await _client.GetMeasurementInfo(id, cancellationToken);
        var timeOfIRStartSeconds = TimeSpan.FromSeconds(info.TimeOfIRStartSeconds);

        var command = new
        {
            command = "Offset t=0",
            parameters = new
            {
                unit = "Seconds",
                offset = timeOfIRStartSeconds.TotalSeconds,
            }
        };
        await _client.ExecuteCommand(id, command, cancellationToken);
    }
}