using System.IO.Ports;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly:InternalsVisibleTo("Shumozavr.RotatingTable.Client")]
[assembly:InternalsVisibleTo("Shumozavr.RotatingTable.Emulator")]
namespace Shumozavr.RotatingTable.Common;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBaseRotatingTableClient(this IServiceCollection services, Func<IServiceProvider, IOptions<RotatingTableSettings>> getSettings)
    {
        services.AddTransient<SerialPort>(
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
        return services;
    }
}