using System.IO.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shumozavr.REW.RotatingTableClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRotatingTableClient(this IServiceCollection services, Func<IServiceProvider, IOptions<RotatingTableClientSettings>> getSettings)
    {
        services.AddSingleton<SerialPort>(
            p =>
            {
                var settings = getSettings(p).Value;
                var port = new SerialPort(settings.SerialPort)
                {
                    BaudRate = 115200,
                    ReadTimeout = (int)settings.ReadPortTimeout.TotalMilliseconds,
                    WriteTimeout = (int)settings.WritePortTimeout.TotalMilliseconds,
                };
                port.Open();
                return port;
            });
        services.AddSingleton<IRotatingTableClient, RotatingTableClient>();
        return services;
    }
}