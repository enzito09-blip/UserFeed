using UserFeed.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace UserFeed.Application.UseCases;

public class DeleteCommentUseCase
{
    private readonly IUserCommentRepository _repository;

    public DeleteCommentUseCase(IUserCommentRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(string commentId, string token)
    {
        var userId = ExtractUserIdFromToken(token);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Usuario no autenticado");

        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
            throw new KeyNotFoundException("Comentario no encontrado");

        if (comment.IsDeleted)
            throw new InvalidOperationException("Este comentario ya esta eliminado y no se puede volver a eliminar");

        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("No podes eliminar comentarios de otros usuarios");

        comment.Delete();
        await _repository.UpdateAsync(comment);
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
