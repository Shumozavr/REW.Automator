using Microsoft.Extensions.DependencyInjection;

namespace Shumozavr.Common.Messaging;

public class EventBusFactory
{
    private readonly IServiceProvider _serviceProvider;

    public EventBusFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEventBus<T> Create<T>()
    {
        return ActivatorUtilities.CreateInstance<InMemoryEventBus<T>>(_serviceProvider);
    }
}