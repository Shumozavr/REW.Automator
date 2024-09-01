using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Shumozavr.REW.Client.Http;
using Shumozavr.RotatingTable.Client;

namespace Shumozavr.REW.Automator.API;

public class InitService : BackgroundService
{
    private readonly ILogger<InitService> _logger;
    private readonly IServer _server;
    private readonly RewMeasureClient _measureClient;
    private readonly RewMeasurementClient _measurementClient;
    private readonly RewApplicationClient _applicationClient;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IRotatingTableClient _rotatingTableClient;

    public InitService(
        ILogger<InitService> logger,
        IServer server,
        RewMeasureClient measureClient,
        RewMeasurementClient measurementClient,
        RewApplicationClient applicationClient,
        IHostApplicationLifetime lifetime,
        IRotatingTableClient rotatingTableClient)
    {
        _logger = logger;
        _server = server;
        _measureClient = measureClient;
        _measurementClient = measurementClient;
        _applicationClient = applicationClient;
        _lifetime = lifetime;
        _rotatingTableClient = rotatingTableClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Initializing...");
        if (!await WaitForAppStartup(_lifetime, stoppingToken))
        {
            _logger.LogError("Web server failed to startup");
            _lifetime.StopApplication();
            return;
        }

        var initCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        initCts.CancelAfter(TimeSpan.FromSeconds(10));

        var serverUrl = new Uri(_server.Features.Get<IServerAddressesFeature>()!.Addresses.First());
        var success = await InitREW(initCts.Token, serverUrl);
        success &= await InitRotatingTable(initCts.Token);
        if (!success)
        {
            _logger.LogError("Application startup failed");
            _lifetime.StopApplication();
        }
        else
        {
            _logger.LogInformation("Application started on {serverUrl}swagger", serverUrl);
        }
    }

    private async Task<bool> InitRotatingTable(CancellationToken cancellationToken)
    {
        try
        {
            await _rotatingTableClient.GetAcceleration(cancellationToken);
            _logger.LogInformation("Rotating Table initialized");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("АЛЛО А РОТЕЙТИНГ ТЕЙБЛ ПОДКЛЮЧЕН??");
            return false;
        }
    }

    private async Task<bool> InitREW(CancellationToken cancellationToken, Uri serverUrl)
    {
        var measureSubscribeUri = new Uri(serverUrl, "rew/measure/subscribe");
        var measurementSubscribeUri = new Uri(serverUrl, "rew/measurements/subscribe");
        var errorsSubscribeUri = new Uri(serverUrl, "rew/application/errors/subscribe");
        var warningsSubscribeUri = new Uri(serverUrl, "rew/application/warnings/subscribe");

        try
        {
            await _measureClient.SubscribeOnEvents(measureSubscribeUri, cancellationToken);
            _lifetime.ApplicationStopping.Register(
                () => _measureClient.Unsubscribe(measureSubscribeUri, _lifetime.ApplicationStopped).Wait(cancellationToken));

            await _measurementClient.SubscribeOnEvents(measurementSubscribeUri, cancellationToken);
            _lifetime.ApplicationStopping.Register(
                () => _measurementClient.Unsubscribe(measurementSubscribeUri, _lifetime.ApplicationStopped).Wait(cancellationToken));

            await _applicationClient.SubscribeOnErrorEvents(errorsSubscribeUri, cancellationToken);
            _lifetime.ApplicationStopping.Register(
                () => _applicationClient.UnsubscribeErrors(errorsSubscribeUri, _lifetime.ApplicationStopped).Wait(cancellationToken));

            await _applicationClient.SubscribeOnWarningEvents(warningsSubscribeUri, cancellationToken);
            _lifetime.ApplicationStopping.Register(
                () => _applicationClient.UnsubscribeWarnings(warningsSubscribeUri, _lifetime.ApplicationStopped).Wait(cancellationToken));

            _logger.LogInformation("Subscribed to REW events");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("АЛЛО А ТЫ REW ЗАПУСТИЛ??????");
            return false;
        }
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