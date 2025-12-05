using UserFeed.Domain.Interfaces;

namespace UserFeed.Application.DTOs;

/// <summary>
/// Response de validación de artículo desde Catalog mediante RabbitMQ
/// </summary>
public class ArticleExistResponse
{
    /// <summary>ID del artículo consultado</summary>
    public string ArticleId { get; set; } = string.Empty;

    /// <summary>ID de correlación para matching request-reply</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>True si el artículo existe y está habilitado</summary>
    public bool Exists { get; set; }

    /// <summary>Detalles del artículo si existe</summary>
    public CatalogArticle? Article { get; set; }

    /// <summary>Mensaje de error si ocurrió un problema</summary>
    public string? Error { get; set; }
}
