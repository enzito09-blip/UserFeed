using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserFeed.Application.DTOs;
using UserFeed.Application.UseCases;

namespace UserFeed.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly CreateCommentUseCase _createComment;
    private readonly GetCommentsByArticleUseCase _getCommentsByArticle;
    private readonly UpdateCommentUseCase _updateComment;
    private readonly DeleteCommentUseCase _deleteComment;
    private readonly GetArticlesWithCommentsUseCase _getArticlesWithComments;

    public CommentsController(
        CreateCommentUseCase createComment,
        GetCommentsByArticleUseCase getCommentsByArticle,
        UpdateCommentUseCase updateComment,
        DeleteCommentUseCase deleteComment,
        GetArticlesWithCommentsUseCase getArticlesWithComments)
    {
        _createComment = createComment;
        _getCommentsByArticle = getCommentsByArticle;
        _updateComment = updateComment;
        _deleteComment = deleteComment;
        _getArticlesWithComments = getArticlesWithComments;
    }

    /// <summary>
    /// Crear un nuevo comentario sobre un artículo
    /// </summary>
    [HttpPost]
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
    /// Obtener lista de artículos con comentarios (solo para pruebas internas)
    /// </summary>
    [HttpGet("articles")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ArticleInfoResponse>>> GetArticlesWithComments()
    {
        try
        {
            var result = await _getArticlesWithComments.ExecuteAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
        }
    }

    /// <summary>
    /// Obtener comentarios de un artículo
    /// </summary>
    [HttpGet("article/{articleId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CommentResponse>>> GetCommentsByArticle(
        string articleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _getCommentsByArticle.ExecuteAsync(articleId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar un comentario existente
    /// </summary>
    [HttpPut("{id}")]
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
