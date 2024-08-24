using Refit;
using Shumozavr.REW.Client.Http.Models;
using Shumozavr.REW.Client.Http.Models.Measure;

namespace Shumozavr.REW.Client.Http;

public interface IRewMeasureHttpClient
{
    [Post("/measure/subscribe")]
    public Task<string> SubscribeOnEvents(SubscribeRequest request, CancellationToken cancellationToken);

    [Post("/measure/naming")]
    public Task SetMeasureName(SetMeasureRequest request, CancellationToken cancellationToken);

    [Post("/measure/command")]
    public Task ExecuteCommand(object command, CancellationToken cancellationToken);

    [Post("/measure/sweep/configuration")]
    public Task SweepConfigure(SweepConfigurationRequest request, CancellationToken cancellationToken);
}