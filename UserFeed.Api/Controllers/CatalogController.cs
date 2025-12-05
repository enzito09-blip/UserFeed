using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserFeed.Domain.Interfaces;

namespace UserFeed.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Tags("Catalogo")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>
    /// Verificar si un artículo existe en el catálogo
    /// </summary>
    /// <param name="articleId">ID del artículo a verificar</param>
    /// <returns>True si el artículo existe y está habilitado, False en caso contrario</returns>
    [HttpGet("articles/{articleId}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CheckArticleExists(string articleId)
    {
        try
        {
            // Extraer token del header para propagarlo al catálogo
            var token = Request.Headers["Authorization"].ToString();
            var exists = await _catalogService.ArticleExistsAsync(articleId, token);
            return Ok(new { articleId, exists });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al verificar artículo", detail = ex.Message });
        }
    }
}
