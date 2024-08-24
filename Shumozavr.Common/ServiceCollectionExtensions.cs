using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shumozavr.Common.Messaging;
using Shumozavr.Common.SerialPorts;
using Shumozavr.Common.SerialPorts.Mock;

namespace Shumozavr.Common;

public static class ServiceCollectionExtensions
{
    //TODO: конфигурация опшенов полная херобора
    public static OptionsBuilder<T> AddDefaultOptions<T>(this IServiceCollection services, Func<IConfiguration, IConfiguration> getConfigurationSection)
        where T : class, IOptionsValue
    {
        return services
              .AddOptions<T>()
              .Configure((T s, IConfiguration c) => getConfigurationSection(c).Bind(s))
              .ValidateDataAnnotations()
              .ValidateOnStart();
    }

    public static OptionsBuilder<T> AddDefaultOptions<T>(this IServiceCollection services, string optionsName, Func<IConfiguration, IConfiguration> getConfigurationSection)
        where T : class, IOptionsValue
    {
        return services
              .AddOptions<T>(optionsName)
              .Configure((T s, IConfiguration c) => getConfigurationSection(c).Bind(s))
              .ValidateDataAnnotations()
              .ValidateOnStart();
    }

    /// <summary>
    /// Adds in memory messaging
    /// </summary>
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        services.TryAddSingleton<EventBusFactory>();
        return services;
    }

    public static IServiceCollection AddSerialPortWrapper<TService>(this IServiceCollection services, Func<IConfiguration, IConfiguration> configureOptions)
    {
        services.AddInMemoryEventBus();
        var serviceKey = typeof(TService).Name;

        services.AddDefaultOptions<SerialPortSettings>(optionsName: serviceKey, configureOptions);

        services.AddKeyedTransient<ISerialPort, SerialPortWrapper>(
            serviceKey,
            (provider, _) => ActivatorUtilities.CreateInstance<SerialPortWrapper>(
                provider,
                serviceKey));
        return services;
    }

    public static IServiceCollection ReplaceSerialPortToMock<TClient, TServer>(this IServiceCollection services)
    {
        services.RemoveAll<ISerialPort>();

        var service1Key = typeof(TClient).Name;
        var service2Key = typeof(TServer).Name;

        services.AddSingleton<SerialPortMock>();
        services.AddKeyedTransient<ISerialPort>(
            service1Key,
            (provider, _) => provider.GetRequiredService<SerialPortMock>().ClientEnd);

        services.AddKeyedTransient<ISerialPort>(
            service2Key,
            (provider, _) => provider.GetRequiredService<SerialPortMock>().ServerEnd);
        return services;
    }
}