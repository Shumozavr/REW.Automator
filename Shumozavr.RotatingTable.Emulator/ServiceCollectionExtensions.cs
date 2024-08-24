using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shumozavr.Common;

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
}