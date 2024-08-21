using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
}