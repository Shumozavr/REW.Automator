using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shumozavr.REW.Client.Http.Models;

namespace Shumozavr.REW.Client.Http;

public class RewApplicationClient
{
    private readonly IRewApplicationHttpClient _client;
    private readonly ILogger<RewApplicationClient> _logger;

    public RewApplicationClient(IRewApplicationHttpClient client, ILogger<RewApplicationClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SubscribeOnErrorEvents(Uri callbackUri, CancellationToken cancellationToken)
    {
        await _client.SubscribeOnErrorsEvents(new SubscribeRequest(callbackUri.ToString()), cancellationToken);
    }

    public async Task UnsubscribeErrors(Uri callbackUri, CancellationToken cancellationToken)
    {
        await _client.UnsubscribeErrors(new SubscribeRequest(callbackUri.ToString()), cancellationToken);
    }

    public Task ErrorCallback(DynamicRewMessage message, CancellationToken cancellationToken)
    {
        _logger.LogError((string)JsonSerializer.Serialize(message.Message));
        return Task.CompletedTask;
    }

    public async Task SubscribeOnWarningEvents(Uri callbackUri, CancellationToken cancellationToken)
    {
        await _client.SubscribeOnWarningsEvents(new SubscribeRequest(callbackUri.ToString()), cancellationToken);
    }

    public async Task UnsubscribeWarnings(Uri callbackUri, CancellationToken cancellationToken)
    {
        await _client.UnsubscribeWarnings(new SubscribeRequest(callbackUri.ToString()), cancellationToken);
    }

    public Task WarningCallback(DynamicRewMessage message, CancellationToken cancellationToken)
    {
        _logger.LogWarning((string)JsonSerializer.Serialize(message.Message));
        return Task.CompletedTask;
    }
}