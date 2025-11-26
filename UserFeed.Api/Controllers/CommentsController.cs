using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserFeed.Application.DTOs;
using UserFeed.Application.UseCases;

namespace UserFeed.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly CreateCommentUseCase _createComment;
    private readonly GetCommentsByArticleUseCase _getCommentsByArticle;
    private readonly UpdateCommentUseCase _updateComment;
    private readonly DeleteCommentUseCase _deleteComment;
    private readonly GetCommentsByUserUseCase _getCommentsByUser;

    public CommentsController(
        CreateCommentUseCase createComment,
        GetCommentsByArticleUseCase getCommentsByArticle,
        UpdateCommentUseCase updateComment,
        DeleteCommentUseCase deleteComment,
        GetCommentsByUserUseCase getCommentsByUser)
    {
        _createComment = createComment;
        _getCommentsByArticle = getCommentsByArticle;
        _updateComment = updateComment;
        _deleteComment = deleteComment;
        _getCommentsByUser = getCommentsByUser;
    }

    /// <summary>
    /// Crear un nuevo comentario sobre un artículo.
    /// </summary>
    /// <remarks>
    /// Reglas:
    /// - Comment: requerido, máximo 500 caracteres.
    /// - Rating: entero entre 1 y 5.
    /// - Se valida que el usuario haya comprado el artículo y no exista comentario previo.
    /// </remarks>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<CommentResponse>> CreateComment([FromBody] CreateCommentRequest request)
    {
        try
        {
            var token = GetTokenFromHeader();
            var result = await _createComment.ExecuteAsync(request, token);
            return CreatedAtAction(nameof(GetCommentsByArticle), new { articleId = result.ArticleId }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
        }
    }

    /// <summary>
    /// Obtener todos los comentarios del usuario autenticado
    /// </summary>
    [HttpGet("my-comments")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CommentResponse>>> GetMyComments()
    {
        try
        {
            var token = GetTokenFromHeader();
            var result = await _getCommentsByUser.ExecuteAsync(token);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
        }
    }

    /// <summary>
    /// Obtener comentarios de un artículo.
    /// </summary>
    /// <remarks>
    /// Parámetros:
    /// - page: número de página (>=1).
    /// - pageSize: valores permitidos 10, 20, 50, 80 o 100.
    /// No se incluyen comentarios eliminados (IsDeleted=true).
    /// </remarks>
    [HttpGet("article/{articleId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CommentResponse>>> GetCommentsByArticle(
        string articleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var allowedPageSizes = new[] { 10, 20, 50, 80, 100 };
            if (!allowedPageSizes.Contains(pageSize))
            {
                return BadRequest(new { message = "pageSize inválido. Valores permitidos: 10,20,50,80,100" });
            }
            if (page < 1)
            {
                return BadRequest(new { message = "page debe ser mayor o igual a 1" });
            }
            var result = await _getCommentsByArticle.ExecuteAsync(articleId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar un comentario existente.
    /// </summary>
    /// <remarks>
    /// Reglas:
    /// - Comment: requerido, máximo 500 caracteres.
    /// - Rating: entero entre 1 y 5.
    /// - No se puede actualizar si el comentario está eliminado.
    /// - Solo el autor puede actualizarlo.
    /// </remarks>
    [HttpPut("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<CommentResponse>> UpdateComment(string id, [FromBody] UpdateCommentRequest request)
    {
        try
        {
            var token = GetTokenFromHeader();
            var result = await _updateComment.ExecuteAsync(id, request, token);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
        }
    }

    /// <summary>
    /// Eliminar un comentario
    /// </summary>
    [HttpDelete("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult> DeleteComment(string id)
    {
        try
        {
            var token = GetTokenFromHeader();
            await _deleteComment.ExecuteAsync(id, token);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
        }
    }

    private string GetTokenFromHeader()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            throw new UnauthorizedAccessException("Token no proporcionado");

        return authHeader.Substring("Bearer ".Length).Trim();
    }
}
