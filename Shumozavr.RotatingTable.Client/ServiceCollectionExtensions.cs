using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shumozavr.Common;

namespace Shumozavr.RotatingTable.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRotatingTableClient(
        this IServiceCollection services,
        Func<IConfiguration, IConfiguration> configureOptions)
    {
        services.AddDefaultOptions<RotatingTableClientSettings>(configureOptions);
        services.AddSerialPortWrapper<RotatingTableClient>(c => configureOptions(c).GetSection(nameof(RotatingTableClientSettings.SerialPort)));
        services.AddSingleton<IRotatingTableClient, RotatingTableClient>();
        return services;
    }
}