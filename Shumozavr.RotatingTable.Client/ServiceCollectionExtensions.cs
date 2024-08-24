using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shumozavr.Common;
using Shumozavr.RotatingTable.Common;

namespace Shumozavr.RotatingTable.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRotatingTableClient(this IServiceCollection services, Func<IServiceProvider, IOptions<RotatingTableSettings>> getSettings)
    {
        services.AddBaseRotatingTableClient(getSettings);
        services.AddInMemoryEventBus<RotatingTableClient>();
        services.AddSingleton<IRotatingTableClient, RotatingTableClient>();
        return services;
    }
}