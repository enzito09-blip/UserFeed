using UserFeed.Application.DTOs;
using UserFeed.Domain.Ports;
using System.IdentityModel.Tokens.Jwt;

namespace UserFeed.Application.UseCases;

public class GetCommentsByUserUseCase
{
    private readonly IUserCommentRepository _repository;

    public GetCommentsByUserUseCase(IUserCommentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CommentResponse>> ExecuteAsync(string token)
    {
        var userId = ExtractUserIdFromToken(token);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Usuario no autenticado");

        var comments = await _repository.GetByUserIdAsync(userId, page: 1, pageSize: 1000);

        return comments.Select(c => new CommentResponse
        {
            Id = c.Id,
            UserId = c.UserId,
            ArticleId = c.ArticleId,
            Comment = c.Comment,
            Rating = c.Rating,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        });
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
