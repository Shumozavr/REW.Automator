using Refit;
using Shumozavr.REW.Client.Http.Models;
using Shumozavr.REW.Client.Http.Models.Measurement;

namespace Shumozavr.REW.Client.Http;

public interface IRewMeasurementHttpClient
{
    [Post("/measurements/subscribe")]
    public Task<string> SubscribeOnEvents(SubscribeRequest request, CancellationToken cancellationToken);

    [Get("/measurements/{id}")]
    public Task<MeasurementInfo> GetMeasurementInfo(string id, CancellationToken cancellationToken);

    [Post("/measurements/{id}/command")]
    public Task ExecuteCommand(string id, object command, CancellationToken cancellationToken);
}