using System.Threading.Channels;
using Shumozavr.REW.Client.Extensions;
using Shumozavr.REW.Client.Http.Models;
using Shumozavr.REW.Client.Http.Models.Measure;

namespace Shumozavr.REW.Client.Http;

public class RewMeasureClient
{
    private readonly IRewMeasureHttpClient _client;
    private readonly Channel<RewMessage> _rewMessages;

    public RewMeasureClient(IRewMeasureHttpClient client)
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

    public async Task<string> Measure(string title, CancellationToken cancellationToken)
    {
        await _client.SetMeasureName(new SetMeasureRequest(title), cancellationToken);
        // TODO: Error handling?
        var resultTask = _rewMessages.WaitForMessage("100% Measurement complete", cancellationToken);

        var command = new { command = "SPL" };
        await _client.ExecuteCommand(command, cancellationToken);
        await resultTask;
        // id
        return "1";
    }
}