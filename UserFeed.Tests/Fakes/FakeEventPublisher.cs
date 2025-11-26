using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserFeed.Domain.Ports;

namespace UserFeed.Tests.Fakes;

public class FakeEventPublisher : IEventPublisher
{
    private readonly List<object> _publishedEvents = new();

    public Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        _publishedEvents.Add(message!);
        return Task.CompletedTask;
    }

    // Test helpers
    public IEnumerable<object> GetPublishedEvents()
    {
        return _publishedEvents;
    }

    public IEnumerable<T> GetPublishedEvents<T>() where T : class
    {
        return _publishedEvents.OfType<T>();
    }

    public void Clear()
    {
        _publishedEvents.Clear();
    }
}
