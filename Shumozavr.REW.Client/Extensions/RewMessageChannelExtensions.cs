using System.Threading.Channels;

namespace Shumozavr.REW.Client.Extensions;

public static class RewMessageChannelExtensions
{
    public static async Task<RewMessage> WaitForMessage(
        this Channel<RewMessage> channel,
        string expectedMessage,
        CancellationToken cancellationToken)
    {
        await foreach (var rewMessage in channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (rewMessage.Message == expectedMessage)
            {
                return rewMessage;
            }
        }

        throw new InvalidOperationException($"Message '{expectedMessage}' was not received.");
    }
}