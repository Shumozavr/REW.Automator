namespace Shumozavr.Common.Messaging;

public interface IEventBus<TMessage>
{
    void TryComplete(Exception? e = null);
    Task<Subscription<TMessage>> Subscribe();
    void Publish(TMessage message);
}