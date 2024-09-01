using System.Threading.Channels;
using Shumozavr.Common.Messaging;
using Shumozavr.REW.Client.Http.Models;
using Shumozavr.REW.Client.Http.Models.Measure;

namespace Shumozavr.REW.Client.Http;

public class RewMeasureClient
{
    private readonly IRewMeasureHttpClient _client;
    private readonly IEventBus<RewMessage> _eventBus;

    public RewMeasureClient(IRewMeasureHttpClient client, EventBusFactory eventBusFactory)
    {
        _client = client;
        _eventBus = eventBusFactory.Create<RewMessage>();
    }

    public async Task SubscribeOnEvents(Uri callbackUri, CancellationToken cancellationToken)
    {
        await _client.SubscribeOnEvents(new SubscribeRequest(callbackUri.ToString()), cancellationToken);
    }

    public async Task Unsubscribe(Uri callbackUri, CancellationToken cancellationToken)
    {
        await _client.SubscribeOnEvents(new SubscribeRequest(callbackUri.ToString()), cancellationToken);
    }

    public Task Callback(RewMessage message, CancellationToken cancellationToken)
    {
        // TODO: надо замутить ивентбас чтобы не держать сообщения если обработчиков нету
        _eventBus.Publish(message);
        return Task.CompletedTask;
    }

    public async Task Measure(string title, string length, CancellationToken cancellationToken)
    {
        using var subscription = await _eventBus.Subscribe();
        await _client.SetMeasureName(new SetMeasureRequest(title, "Use as entered"), cancellationToken);
        await _client.SweepConfigure(new SweepConfigurationRequest(length), cancellationToken);

        var command = new { command = "SPL" };
        await _client.ExecuteCommand(command, cancellationToken);

        // TODO: Error handling?
        await subscription.WaitForMessage(m => m.Message.Contains("100% Measurement complete"), cancellationToken);
    }
}