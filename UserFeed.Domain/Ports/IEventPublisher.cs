namespace UserFeed.Domain.Ports;

public interface IEventPublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message);
}
