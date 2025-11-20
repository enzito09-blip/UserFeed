using UserFeed.Application.DTOs;
using UserFeed.Domain.Ports;

namespace UserFeed.Application.UseCases;

public class UpdateCommentUseCase
{
    private readonly IUserCommentRepository _repository;
    private readonly IAuthService _authService;

    public UpdateCommentUseCase(IUserCommentRepository repository, IAuthService authService)
    {
        _repository = repository;
        _authService = authService;
    }

    public async Task<CommentResponse> ExecuteAsync(string commentId, UpdateCommentRequest request, string token)
    {
        var user = await _authService.GetCurrentUserAsync(token);
        if (user == null)
            throw new UnauthorizedAccessException("Usuario no autenticado");

        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
            throw new KeyNotFoundException("Comentario no encontrado");

        if (comment.UserId != user.Id)
            throw new UnauthorizedAccessException("No puedes modificar comentarios de otros usuarios");

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
}
