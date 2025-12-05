namespace UserFeed.Domain.DTOs;

/// <summary>
/// Request para validar la existencia de un artículo mediante RabbitMQ
/// </summary>
public class ArticleExistRequest
{
    /// <summary>ID del artículo a verificar</summary>
    public string ArticleId { get; set; } = string.Empty;

    /// <summary>ID de correlación para matching request-reply</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Nombre de la queue de respuesta</summary>
    public string ReplyTo { get; set; } = string.Empty;

    /// <summary>Token JWT para autenticación con Catalog Service</summary>
    public string AuthToken { get; set; } = string.Empty;
}
