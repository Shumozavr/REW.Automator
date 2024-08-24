using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Shumozavr.Common.Messaging;

public class InMemoryEventBus<TMessage> : IEventBus<TMessage>
{
    private readonly ILogger<InMemoryEventBus<TMessage>> _logger;
    private readonly List<Channel<TMessage>> _subscriptions = [];

    public InMemoryEventBus(ILogger<InMemoryEventBus<TMessage>> logger)
    {
        _logger = logger;
    }

    public void TryComplete(Exception? e = null)
    {
        foreach (var subscription in _subscriptions)
        {
            if (!subscription.Writer.TryComplete(e))
            {
                _logger.LogWarning("Writer was already completed");
            }
        }
    }
    public void Publish(TMessage message)
    {
        _logger.LogInformation("publishing message {token}", message);
        foreach (var subscription in _subscriptions)
        {
            if (!subscription.Writer.TryWrite(message))
            {
                _logger.LogWarning("Reader can't keep up with writes");
            }
        }
    }

    public Task<Subscription<TMessage>> Subscribe()
    {
        var channel = Channel.CreateUnbounded<TMessage>();
        var subscription = new Subscription<TMessage>(
            channel.Reader,
            disposeAction: () => {
                _logger.LogTrace("disposing subscription");
                _subscriptions.Remove(channel);
                channel.Writer.TryComplete();
                _logger.LogTrace("subscription disposed");
            });

        _subscriptions.Add(channel);

        return Task.FromResult(subscription);
    }
}