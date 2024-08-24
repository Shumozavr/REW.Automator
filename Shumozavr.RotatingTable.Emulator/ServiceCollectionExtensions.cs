using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shumozavr.RotatingTable.Common;

namespace Shumozavr.RotatingTable.Emulator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRotatingTableEmulator(this IServiceCollection services, Func<IServiceProvider, IOptions<RotatingTableSettings>> getSettings)
    {
        services.AddBaseRotatingTableClient(getSettings);
        services.AddSingleton<RotatingTableEmulator>();
        return services;
    }
}