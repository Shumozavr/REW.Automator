namespace Shumozavr.Common.Messaging;

public static class SubscriptionExtensions
{
    public static async Task<T> WaitForMessage<T>(
        this Subscription<T> subscription,
        Func<T, bool> predicate,
        CancellationToken cancellationToken)
    {
        await foreach (var message in subscription.MessagesReader.ReadAllAsync(cancellationToken))
        {
            if (predicate(message))
            {
                return message;
            }
        }

        throw new InvalidOperationException("Message was not received.");
    }
}