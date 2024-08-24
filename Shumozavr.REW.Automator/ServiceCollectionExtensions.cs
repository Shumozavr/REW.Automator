using Microsoft.Extensions.DependencyInjection;

namespace Shumozavr.REW.Automator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutomator(this IServiceCollection services)
    {
        services.AddSingleton<MeasureRecorderService>();
        return services;
    }
}