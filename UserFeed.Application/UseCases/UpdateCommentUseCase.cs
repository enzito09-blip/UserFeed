using UserFeed.Application.DTOs;
using UserFeed.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace UserFeed.Application.UseCases;

public class UpdateCommentUseCase
{
    private readonly IUserCommentRepository _repository;

    public UpdateCommentUseCase(IUserCommentRepository repository)
    {
        _repository = repository;
    }

    public async Task<CommentResponse> ExecuteAsync(string commentId, UpdateCommentRequest request, string token)
    {
        var userId = ExtractUserIdFromToken(token);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Usuario no autenticado");

        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
            throw new KeyNotFoundException("El comentario seleccionado no existe");

        if (comment.IsDeleted)
            throw new InvalidOperationException("El comentario esta eliminado y no puede ser actualizado");

        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("El comentario seleccionado no lo realizo el usuario logueado, por lo que no lo podrá actualizar");

        // Validaciones de entrada
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new ArgumentException("Comment es requerido");
        if (request.Comment.Length > 500)
            throw new ArgumentException("El comentario es demasiado largo, debe tener 500 caracteres o menos");
        if (request.Rating < 1 || request.Rating > 5)
            throw new ArgumentException("Rating debe estar entre 1 y 5");

        comment.Update(request.Comment, request.Rating);
        var updated = await _repository.UpdateAsync(comment);

        return new CommentResponse
        {
            Id = updated.Id,
            UserId = updated.UserId,
            ArticleId = updated.ArticleId,
            Comment = updated.Comment,
            Rating = updated.Rating,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }

    private string? ExtractUserIdFromToken(string authHeader)
    {
        try
        {
            var token = authHeader.Replace("Bearer ", "").Trim();
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c =>
                string.Equals(c.Type, "userId", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "userID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "uid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "sub", StringComparison.OrdinalIgnoreCase));
            return userIdClaim?.Value;
        }
        catch
        {
            return null;
        }
    }
}
