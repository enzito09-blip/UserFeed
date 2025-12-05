namespace UserFeed.Application.DTOs;

/// <summary>
/// Request para validar existencia de artículo mediante RabbitMQ
/// Patrón Request-Reply asíncrono
/// </summary>
public class ArticleExistRequest
{
    /// <summary>ID único del artículo a validar</summary>
    public string ArticleId { get; set; } = string.Empty;

    /// <summary>ID de correlación para matching request-reply</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Queue donde se debe enviar la respuesta</summary>
    public string ReplyTo { get; set; } = string.Empty;
}
