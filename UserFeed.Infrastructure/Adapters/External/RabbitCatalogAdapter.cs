using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UserFeed.Domain.DTOs;
using UserFeed.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace UserFeed.Infrastructure.Adapters.External;

/// <summary>
/// Adapter para consultar artículos del Catalog Service mediante RabbitMQ (patrón Request-Reply asíncrono)
/// Implementa ICatalogService pero usa messaging en lugar de HTTP
/// </summary>
public class RabbitCatalogAdapter : ICatalogService
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitCatalogAdapter>? _logger;
    private readonly string _exchangeName = "catalog";
    private readonly string _requestRoutingKey = "article_exist";
    private readonly TimeSpan _responseTimeout = TimeSpan.FromSeconds(10);

    public RabbitCatalogAdapter(IConnection connection, ILogger<RabbitCatalogAdapter>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger;
    }

    /// <summary>
    /// Verifica si un artículo existe y está habilitado mediante RabbitMQ
    /// Usa patrón Request-Reply: envía mensaje y espera respuesta
    /// </summary>
    public async Task<bool> ArticleExistsAsync(string articleId, string? token = null)
    {
        var response = await GetArticleAsync(articleId, token);
        return response != null;
    }

    /// <summary>
    /// Obtiene detalles de un artículo mediante RabbitMQ Request-Reply
    /// Envía el mensaje en formato PascalCase que el listener espera deserializar
    /// </summary>
    public async Task<CatalogArticle?> GetArticleAsync(string articleId, string? token = null)
    {
        try
        {
            _logger?.LogInformation($"📤 [RabbitCatalogAdapter] Iniciando búsqueda de artículo: {articleId}");
            
            using (var channel = _connection.CreateModel())
            {
                // Declarar exchange directo para catalog
                channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct, durable: true);

                // Crear queue temporal para la respuesta usando topic exchange
                var responseQueueName = $"response_{Guid.NewGuid()}";
                channel.QueueDeclare(queue: responseQueueName, durable: false, exclusive: true, autoDelete: true);
                channel.QueueBind(queue: responseQueueName, exchange: "amq.topic", routingKey: responseQueueName);
                
                var correlationId = Guid.NewGuid().ToString();

                // Extraer solo el token (sin "Bearer ")
                var authToken = token?.Replace("Bearer ", "") ?? string.Empty;
                if (!string.IsNullOrEmpty(authToken))
                {
                    _logger?.LogInformation($"🔑 [RabbitCatalogAdapter] Token recibido del controller");
                }

                // Preparar request en formato PascalCase que el listener espera
                var request = new ArticleExistRequest
                {
                    ArticleId = articleId,
                    CorrelationId = correlationId,
                    ReplyTo = responseQueueName,
                    AuthToken = authToken
                };

                var requestJson = JsonSerializer.Serialize(request);
                var requestBody = Encoding.UTF8.GetBytes(requestJson);

                // Enviar request a Catalog
                var properties = channel.CreateBasicProperties();
                properties.CorrelationId = correlationId;
                properties.ReplyTo = responseQueueName;
                properties.ContentType = "application/json";
                properties.DeliveryMode = 2; // Persistente

                _logger?.LogInformation($"📨 [RabbitCatalogAdapter] Publicando request - CorrelationId: {correlationId}, ReplyTo: {responseQueueName}");
                
                channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: _requestRoutingKey,
                    basicProperties: properties,
                    body: requestBody
                );

                // Esperar respuesta en la queue temporal
                var tcs = new TaskCompletionSource<string>();
                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var responseJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var corrId = ea.BasicProperties?.CorrelationId;
                        
                        if (corrId == correlationId)
                        {
                            tcs.SetResult(responseJson);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"❌ [RabbitCatalogAdapter] Error procesando respuesta: {ex.Message}");
                        tcs.SetException(ex);
                    }
                };

                channel.BasicConsume(queue: responseQueueName, autoAck: true, consumer: consumer);

                // Esperar respuesta con timeout
                var waitForResponse = tcs.Task.Wait(_responseTimeout);
                
                if (waitForResponse)
                {
                    var responseJson = tcs.Task.Result;
                    _logger?.LogInformation($"📥 [RabbitCatalogAdapter] Respuesta JSON recibida: {responseJson}");
                    
                    var responseDict = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
                    
                    if (responseDict != null)
                    {
                        _logger?.LogInformation($"📋 [RabbitCatalogAdapter] Claves disponibles: {string.Join(", ", responseDict.Keys)}");
                        
                        // Buscar la clave "exists" (puede ser "Exists" o "exists" dependiendo del camelCase)
                        var existsKey = responseDict.Keys.FirstOrDefault(k => k.Equals("exists", StringComparison.OrdinalIgnoreCase));
                        
                        if (!string.IsNullOrEmpty(existsKey) && responseDict[existsKey] is JsonElement je)
                        {
                            var exists = je.GetBoolean();
                            
                            _logger?.LogInformation($"✅ [RabbitCatalogAdapter] Artículo encontrado (exists={exists})");
                            
                            if (exists)
                            {
                                var articleKey = responseDict.Keys.FirstOrDefault(k => k.Equals("article", StringComparison.OrdinalIgnoreCase));
                                if (!string.IsNullOrEmpty(articleKey) && responseDict[articleKey] is JsonElement articleJe)
                                {
                                    var articleJson = articleJe.GetRawText();
                                    if (!string.IsNullOrEmpty(articleJson) && articleJson != "null")
                                    {
                                        var article = JsonSerializer.Deserialize<CatalogArticle>(articleJson);
                                        _logger?.LogInformation($"✅ [RabbitCatalogAdapter] Artículo deserializado: {article?.Name}");
                                        return article;
                                    }
                                }
                            }
                            else
                            {
                                _logger?.LogWarning($"⚠️ [RabbitCatalogAdapter] Artículo NO encontrado en respuesta (exists=false)");
                            }
                        }
                        else
                        {
                            _logger?.LogWarning($"⚠️ [RabbitCatalogAdapter] No se encontró la clave 'exists' en la respuesta");
                        }
                    }
                    else
                    {
                        _logger?.LogError($"❌ [RabbitCatalogAdapter] No se pudo deserializar respuesta JSON");
                    }

                    return null;
                }
                else
                {
                    _logger?.LogWarning($"⏱ [RabbitCatalogAdapter] Timeout esperando respuesta - CorrelationId: {correlationId}");
                    return null;
                }
            }
        }
        catch (TimeoutException)
        {
            _logger?.LogError($"⏱ [RabbitCatalogAdapter] TimeoutException esperando respuesta");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"❌ [RabbitCatalogAdapter] Error en comunicación: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Obtiene todos los artículos (no implementado para este adapter RabbitMQ)
    /// </summary>
    public Task<IEnumerable<CatalogArticle>> GetAllArticlesAsync(string? token = null)
    {
        throw new NotImplementedException("GetAllArticlesAsync no está implementado para RabbitCatalogAdapter");
    }
}
