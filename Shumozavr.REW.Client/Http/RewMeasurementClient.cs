using System.Threading.Channels;
using Shumozavr.Common.Messaging;
using Shumozavr.REW.Client.Http.Models;
using Shumozavr.REW.Client.Http.Models.Measurement;

namespace Shumozavr.REW.Client.Http;

public class RewMeasurementClient
{
    private readonly IRewMeasurementHttpClient _client;
    private readonly IEventBus<DynamicRewMessage> _eventBus;

    public RewMeasurementClient(IRewMeasurementHttpClient client, EventBusFactory eventBusFactory)
    {
        _client = client;
        _eventBus = eventBusFactory.Create<DynamicRewMessage>();
    }

    public async Task SubscribeOnEvents(Uri callbackUri, CancellationToken cancellationToken)
    {
        await _client.SubscribeOnEvents(new SubscribeRequest(callbackUri.ToString()), cancellationToken);
    }

    public async Task Unsubscribe(Uri callbackUri, CancellationToken cancellationToken)
    {
        await _client.SubscribeOnEvents(new SubscribeRequest(callbackUri.ToString()), cancellationToken);
    }

    public Task Callback(DynamicRewMessage message, CancellationToken cancellationToken)
    {
        _eventBus.Publish(message);
        return Task.CompletedTask;
    }

    public async Task UpdateIrWindowsSettings(
        string id,
        UpdateIrWindowSettingsRequest request,
        CancellationToken cancellationToken)
    {
        await _client.UpdateIrWindowSettings(id, request, cancellationToken);
    }

    public async Task<string> GetSelectedMeasurementUuid(CancellationToken cancellationToken) =>
        (await _client.GetSelectedMeasurementUuid(cancellationToken)).Message;

    public async Task<string> GetSelectedMeasurementIndex(CancellationToken cancellationToken) =>
        await _client.GetSelectedMeasurementIndex(cancellationToken);

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