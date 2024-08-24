using System.Threading.Channels;
using Shumozavr.Common.Messaging;

namespace Shumozavr.REW.Client.Extensions;

public static class RewMessageChannelExtensions
{
    public static async Task<RewMessage> WaitForMessage(
        this Subscription<RewMessage> subscription,
        string expectedMessage,
        CancellationToken cancellationToken)
    {
        await foreach (var rewMessage in subscription.MessagesReader.ReadAllAsync(cancellationToken))
        {
            if (rewMessage.Message == expectedMessage)
            {
                return rewMessage;
            }
        }

        throw new InvalidOperationException($"Message '{expectedMessage}' was not received.");
    }
}