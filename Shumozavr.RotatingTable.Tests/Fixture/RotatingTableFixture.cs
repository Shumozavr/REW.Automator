using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shumozavr.Common.SerialPorts;
using Shumozavr.RotatingTable.Client;
using Shumozavr.RotatingTable.Emulator;

namespace Shumozavr.RotatingTable.Tests.Fixture;

public class RotatingTableFixture
{
    public readonly IRotatingTableClient Client;
    public readonly RotatingTableEmulator Emulator;

    public RotatingTableFixture()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRotatingTableClient(c => c.GetSection(RotatingTableClientSettings.OptionsKey));
        services.AddRotatingTableEmulator(c => c.GetSection(RotatingTableEmulatorSettings.OptionsKey));
        services.ReplaceSerialPortToMock<RotatingTableClient, RotatingTableEmulator>();

        var serviceProvider = services.BuildServiceProvider();

        Client = serviceProvider.GetRequiredService<IRotatingTableClient>();
        Emulator = serviceProvider.GetRequiredService<RotatingTableEmulator>();
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ReplaceSerialPortToMock<TService1, TService2>(this IServiceCollection services)
    {
        services.RemoveAll<ISerialPort>();

        var service1Key = typeof(TService1).Name;
        var service2Key = typeof(TService2).Name;

        services.AddSingleton<SerialPortMock>();
        services.AddKeyedTransient<ISerialPort>(
            service1Key,
            (provider, _) => provider.GetRequiredService<SerialPortMock>().FirstEnd);

        services.AddKeyedTransient<ISerialPort>(
            service2Key,
            (provider, _) => provider.GetRequiredService<SerialPortMock>().SecondEnd);
        return services;
    }
}