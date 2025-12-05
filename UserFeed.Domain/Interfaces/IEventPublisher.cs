namespace UserFeed.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message);
}
