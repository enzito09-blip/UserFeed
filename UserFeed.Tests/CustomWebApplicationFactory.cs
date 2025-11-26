using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UserFeed.Domain.Ports;
using UserFeed.Tests.Fakes;

namespace UserFeed.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeUserCommentRepository CommentRepository { get; } = new();
    public FakeCatalogService CatalogService { get; } = new();
    public FakeOrderService OrderService { get; } = new();
    public FakeEventPublisher EventPublisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing registrations
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(IUserCommentRepository) ||
                           d.ServiceType == typeof(ICatalogService) ||
                           d.ServiceType == typeof(IOrderService) ||
                           d.ServiceType == typeof(IEventPublisher))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add fakes
            services.AddSingleton<IUserCommentRepository>(CommentRepository);
            services.AddSingleton<ICatalogService>(CatalogService);
            services.AddSingleton<IOrderService>(OrderService);
            services.AddSingleton<IEventPublisher>(EventPublisher);
        });
    }

    public void Reset()
    {
        CommentRepository.Clear();
        CatalogService.Clear();
        OrderService.Clear();
        EventPublisher.Clear();
    }
}
