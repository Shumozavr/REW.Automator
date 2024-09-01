using Refit;
using Shumozavr.REW.Client.Http.Models;

namespace Shumozavr.REW.Client.Http;

public interface IRewApplicationHttpClient
{
    [Post("/application/errors/subscribe")]
    public Task SubscribeOnErrorsEvents(SubscribeRequest request, CancellationToken cancellationToken);

    [Post("/application/errors/unsubscribe")]
    public Task<string> UnsubscribeErrors(SubscribeRequest request, CancellationToken cancellationToken);

    [Post("/application/warnings/subscribe")]
    public Task SubscribeOnWarningsEvents(SubscribeRequest request, CancellationToken cancellationToken);

    [Post("/application/warnings/unsubscribe")]
    public Task<string> UnsubscribeWarnings(SubscribeRequest request, CancellationToken cancellationToken);
}