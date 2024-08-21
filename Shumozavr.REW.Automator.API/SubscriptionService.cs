using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Shumozavr.REW.Client.Http;

namespace Shumozavr.REW.Automator.API;

public class SubscriptionService : BackgroundService
{
    private readonly IServer _server;
    private readonly RewMeasureClient _measureClient;
    private readonly RewMeasurementClient _measurementClient;
    private readonly IHostApplicationLifetime _lifetime;

    public SubscriptionService(IServer server, RewMeasureClient measureClient, RewMeasurementClient measurementClient, IHostApplicationLifetime lifetime)
    {
        _server = server;
        _measureClient = measureClient;
        _measurementClient = measurementClient;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await WaitForAppStartup(_lifetime, stoppingToken))
        {
            return;
        }

        var serverUrl = new Uri(_server.Features.Get<IServerAddressesFeature>()!.Addresses.First());

        var measureSubscribeUri = new Uri(serverUrl, "rew/measure/subscribe");
        var measurementSubscribeUri = new Uri(serverUrl, "rew/measurements/subscribe");

        await _measureClient.SubscribeOnEvents(measureSubscribeUri, _lifetime.ApplicationStopping);
        await _measurementClient.SubscribeOnEvents(measurementSubscribeUri, _lifetime.ApplicationStopping);
    }

    static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        var cancelledSource = new TaskCompletionSource();

        await using var reg1 = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());
        await using var reg2 = stoppingToken.Register(() => cancelledSource.SetResult());

        var completedTask = await Task.WhenAny(
            startedSource.Task,
            cancelledSource.Task).ConfigureAwait(false);

        return completedTask == startedSource.Task;
    }
}