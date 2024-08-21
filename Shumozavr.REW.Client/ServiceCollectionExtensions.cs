using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Shumozavr.REW.Client.Http;

namespace Shumozavr.REW.Client;

public static class ServiceCollectionExtensions
{
    public static void AddRewClient(
        this IServiceCollection services,
        Func<IServiceProvider, IOptions<RewClientSettings>> getSettings)
    {
        services.AddRefitClient<IRewMeasurementHttpClient>().ConfigureHttpClient(
            (p, o) =>
            {
                var settings = getSettings(p);
                o.BaseAddress = new Uri(settings.Value.BaseAddress);
            });
        services.AddRefitClient<IRewMeasureHttpClient>().ConfigureHttpClient(
            (p, o) =>
            {
                var settings = getSettings(p);
                o.BaseAddress = new Uri(settings.Value.BaseAddress);
            });

        services.AddSingleton<RewMeasureClient>();
        services.AddSingleton<RewMeasurementClient>();
    }
}