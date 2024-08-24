using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Shumozavr.Common;
using Shumozavr.REW.Client.Http;

namespace Shumozavr.REW.Client;

public static class ServiceCollectionExtensions
{
    public static void AddRewClient(
        this IServiceCollection services,
        Func<IConfiguration, IConfiguration> getSettings)
    {
        services.AddDefaultOptions<RewClientSettings>(getSettings);
        services.AddRefitClient<IRewMeasurementHttpClient>().ConfigureHttpClient(
            (p, o) =>
            {
                var settings = p.GetRequiredService<IOptions<RewClientSettings>>().Value;
                o.BaseAddress = new Uri(settings.BaseAddress);
            });
        services.AddRefitClient<IRewMeasureHttpClient>().ConfigureHttpClient(
            (p, o) =>
            {
                var settings = p.GetRequiredService<IOptions<RewClientSettings>>().Value;
                o.BaseAddress = new Uri(settings.BaseAddress);
            });

        services.AddSingleton<RewMeasureClient>();
        services.AddSingleton<RewMeasurementClient>();
    }
}