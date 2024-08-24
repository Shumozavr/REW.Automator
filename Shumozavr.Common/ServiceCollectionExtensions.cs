using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shumozavr.Common.Messaging;

namespace Shumozavr.Common;

public static class ServiceCollectionExtensions
{
    public static OptionsBuilder<T> AddDefaultOptions<T>(this IServiceCollection services)
        where T : class, IOptionsValue
    {
        return services
              .AddOptions<T>()
              .BindConfiguration(T.OptionsKey)
              .ValidateDataAnnotations()
              .ValidateOnStart();
    }


    /// <summary>
    /// Adds in memory messaging
    /// </summary>
    /// <typeparam name="TService">Unique type for separating one event bus from another</typeparam>
    public static IServiceCollection AddInMemoryEventBus<TService>(this IServiceCollection services)
    {
        services.AddKeyedSingleton(typeof(IEventBus<>), typeof(TService).Name, typeof(InMemoryEventBus<>));
        return services;
    }
}