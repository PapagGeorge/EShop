using EShop.Ordering.Application.Interfaces;
using MassTransit;

namespace EShop.Ordering.Infrastructure.Services;

public class EventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : class
    {
        await _publishEndpoint.Publish(@event, cancellationToken);
    }
}
