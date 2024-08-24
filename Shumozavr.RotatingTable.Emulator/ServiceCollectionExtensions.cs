using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shumozavr.Common;
using Shumozavr.RotatingTable.Client;

namespace Shumozavr.RotatingTable.Emulator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRotatingTableEmulator(
        this IServiceCollection services,
        Func<IConfiguration, IConfiguration> configureOptions)
    {
        services.AddDefaultOptions<RotatingTableEmulatorSettings>(configureOptions);
        services.AddSerialPortWrapper<RotatingTableEmulator>(c => configureOptions(c).GetSection(nameof(RotatingTableEmulatorSettings.SerialPort)));
        services.AddSingleton<RotatingTableEmulator>();
        return services;
    }

    public static void AddRotatingTable(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRotatingTableClient(c => c.GetSection(RotatingTableClientSettings.OptionsKey));
        var settings = configuration.GetSection(RotatingTableEmulatorSettings.OptionsKey).Get<RotatingTableEmulatorSettings>();
        if (settings?.Enabled == true)
        {
            services.AddHostedService<RotatingTableEmulatorService>();
            services.AddRotatingTableEmulator(c => c.GetSection(RotatingTableEmulatorSettings.OptionsKey));
            if (settings.UseMock)
            {
                services.ReplaceSerialPortToMock<RotatingTableClient, RotatingTableEmulator>();
            }
        }
    }
}