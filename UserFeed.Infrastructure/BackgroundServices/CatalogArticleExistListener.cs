using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UserFeed.Domain.DTOs;
using UserFeed.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UserFeed.Infrastructure.BackgroundServices;

/// <summary>
/// Servicio de background que simula el listener del Catalog Service
/// Escucha requests de "article_exist" y responde con la información del artículo
/// 
/// En producción, este servicio estaría en el Catalog Service (Go)
/// Aquí lo implementamos para demostrar el patrón Request-Reply con RabbitMQ
/// </summary>
public class CatalogArticleExistListener : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<CatalogArticleExistListener> _logger;
    private readonly HttpClient _httpClient;
    private IModel _channel;
    private const string ExchangeName = "catalog";
    private const string QueueName = "article_exist_queue";
    private const string RoutingKey = "article_exist";
    private const string CatalogServiceUrl = "http://localhost:3002/articles";

    public CatalogArticleExistListener(IConnection connection, ILogger<CatalogArticleExistListener> logger, HttpClient httpClient)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🔄 Inicializando CatalogArticleExistListener...");
            
            _channel = _connection.CreateModel();
            _logger.LogInformation("✅ Conexión a RabbitMQ establecida");

            // Declarar exchange y queue para article_exist
            _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);

            _logger.LogInformation($"✅ Escuchando en queue '{QueueName}' para requests de article_exist");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) => await OnMessageReceivedAsync(ea, stoppingToken);

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

            // Mantener el servicio corriendo
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Servicio CatalogArticleExistListener detenido por cancelación");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error CRÍTICO en CatalogArticleExistListener: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    private async Task OnMessageReceivedAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        try
        {
            var requestJson = Encoding.UTF8.GetString(ea.Body.ToArray());
            var request = JsonSerializer.Deserialize<ArticleExistRequest>(requestJson);

            _logger.LogInformation($"📨 Request recibido - ArticleId: {request?.ArticleId}, CorrelationId: {request?.CorrelationId}");

            // Consultar Catalog Service real a través de HTTP
            var article = await GetArticleFromCatalogServiceAsync(request?.ArticleId, request?.AuthToken);

            // Preparar response
            var response = new ArticleExistResponse
            {
                ArticleId = request?.ArticleId ?? string.Empty,
                CorrelationId = request?.CorrelationId ?? string.Empty,
                Exists = article != null,
                Article = article,
                Error = article == null ? "Artículo no encontrado" : null
            };

            // Enviar response a la queue de respuesta
            if (!string.IsNullOrEmpty(request?.ReplyTo))
            {
                var responseJson = JsonSerializer.Serialize(response);
                var responseBody = Encoding.UTF8.GetBytes(responseJson);

                var responseProperties = _channel.CreateBasicProperties();
                responseProperties.CorrelationId = request.CorrelationId;
                responseProperties.ContentType = "application/json";

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: request.ReplyTo,
                    basicProperties: responseProperties,
                    body: responseBody
                );

                _logger.LogInformation($"📤 Response enviado - Existe: {response.Exists}");
            }

            // Confirmar el mensaje
            _channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error procesando mensaje: {ex.Message}");
            // Rechazar el mensaje sin requeue (ir a DLQ si existe)
            _channel.BasicNack(ea.DeliveryTag, false, false);
        }
    }

    /// <summary>
    /// Realiza una llamada HTTP real al Catalog Service para obtener información del artículo
    /// Endpoint: GET http://localhost:3002/api/v1/articles/{articleId}
    /// </summary>
    private async Task<CatalogArticle?> GetArticleFromCatalogServiceAsync(string? articleId, string? authToken = null)
    {
        try
        {
            if (string.IsNullOrEmpty(articleId))
            {
                _logger.LogWarning("ArticleId es nulo o vacío");
                return null;
            }

            var url = $"{CatalogServiceUrl}/{articleId}";
            _logger.LogInformation($"🌐 Consultando Catalog Service: {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            // Agregar token JWT de autorización si está disponible
            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                _logger.LogInformation($"🔑 Token de autorización agregado al request");
            }

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"📭 Artículo no encontrado en Catalog Service: {articleId}");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"❌ Catalog Service retornó {response.StatusCode}: {response.ReasonPhrase} - Content: {errorContent}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"📦 Response del Catalog Service: {content}");

            // Parsear la respuesta - puede venir como { data: {...} } o directamente el objeto
            var catalogResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            JsonElement articleData = default;
            bool foundArticleData = false;

            // Intentar obtener desde "data"
            if (catalogResponse.ValueKind == JsonValueKind.Object && catalogResponse.TryGetProperty("data", out var dataElement))
            {
                articleData = dataElement;
                foundArticleData = true;
                _logger.LogInformation("📍 Artículo encontrado en propiedad 'data'");
            }
            // Si no existe "data", usar el objeto raíz si tiene las propiedades esperadas
            else if (catalogResponse.ValueKind == JsonValueKind.Object && catalogResponse.TryGetProperty("_id", out _))
            {
                articleData = catalogResponse;
                foundArticleData = true;
                _logger.LogInformation("📍 Artículo encontrado en objeto raíz");
            }

            if (!foundArticleData)
            {
                _logger.LogWarning($"❌ No se pudo encontrar datos del artículo. Respuesta: {content}");
                return null;
            }

            var article = new CatalogArticle
            {
                Id = GetPropertyString(articleData, new[] { "_id", "id" }) ?? "",
                Name = GetPropertyString(articleData, new[] { "name" }) ?? "",
                Description = GetPropertyString(articleData, new[] { "description" }) ?? "",
                Image = GetPropertyString(articleData, new[] { "image" }) ?? "",
                Price = GetPropertyInt(articleData, new[] { "price" }) ?? 0,
                Stock = GetPropertyInt(articleData, new[] { "stock" }) ?? 0,
                Enabled = GetPropertyBool(articleData, new[] { "enabled" }) ?? true
            };

            if (string.IsNullOrEmpty(article.Id))
            {
                _logger.LogWarning("❌ No se pudo extraer ID del artículo");
                return null;
            }

            _logger.LogInformation($"✅ Artículo encontrado: {article.Name} (ID: {article.Id}) - Price: {article.Price}, Stock: {article.Stock}");
            return article;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"❌ Error de conexión con Catalog Service: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError($"❌ Error deserializando respuesta del Catalog Service: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error inesperado consultando Catalog Service: {ex.Message}");
            return null;
        }
    }

    private string? GetPropertyString(JsonElement element, string[] propertyNames)
    {
        foreach (var prop in propertyNames)
        {
            if (element.TryGetProperty(prop, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }
        return null;
    }

    private int? GetPropertyInt(JsonElement element, string[] propertyNames)
    {
        foreach (var prop in propertyNames)
        {
            if (element.TryGetProperty(prop, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number)
                {
                    return value.GetInt32();
                }
                // Intentar parsear como double y convertir a int
                if (value.TryGetDouble(out var dValue))
                {
                    return (int)dValue;
                }
            }
        }
        return null;
    }

    private bool? GetPropertyBool(JsonElement element, string[] propertyNames)
    {
        foreach (var prop in propertyNames)
        {
            if (element.TryGetProperty(prop, out var value) && value.ValueKind == JsonValueKind.True)
            {
                return true;
            }
            else if (element.TryGetProperty(prop, out value) && value.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }
        return null;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _logger.LogInformation("CatalogArticleExistListener detenido");
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
