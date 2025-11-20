using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UserFeed.Domain.Ports;
using UserFeed.Infrastructure.Configuration;

namespace UserFeed.Infrastructure.Adapters.Messaging;

public class LogoutConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IAuthService _authService;
    private readonly RabbitMqSettings _settings;

    public LogoutConsumer(RabbitMqSettings settings, IAuthService authService)
    {
        _settings = settings;
        _authService = authService;

        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_settings.LogoutExchange, ExchangeType.Fanout, durable: true);
        var queueName = _channel.QueueDeclare().QueueName;
        _channel.QueueBind(queueName, _settings.LogoutExchange, "");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += OnLogoutReceived;
        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

    private void OnLogoutReceived(object? sender, BasicDeliverEventArgs e)
    {
        try
        {
            var body = Encoding.UTF8.GetString(e.Body.ToArray());
            var logoutEvent = JsonSerializer.Deserialize<LogoutEvent>(body);

            if (logoutEvent?.Token != null)
            {
                _authService.InvalidateTokenCache(logoutEvent.Token);
                Console.WriteLine($"Token invalidado del cache: {logoutEvent.Token.Substring(0, 10)}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando logout event: {ex.Message}");
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }

    private class LogoutEvent
    {
        public string Token { get; set; } = string.Empty;
    }
}
