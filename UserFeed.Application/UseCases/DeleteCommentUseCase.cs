using UserFeed.Domain.Ports;

namespace UserFeed.Application.UseCases;

public class DeleteCommentUseCase
{
    private readonly IUserCommentRepository _repository;
    private readonly IAuthService _authService;

    public DeleteCommentUseCase(IUserCommentRepository repository, IAuthService authService)
    {
        _repository = repository;
        _authService = authService;
    }

    public async Task ExecuteAsync(string commentId, string token)
    {
        var user = await _authService.GetCurrentUserAsync(token);
        if (user == null)
            throw new UnauthorizedAccessException("Usuario no autenticado");

        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
            throw new KeyNotFoundException("Comentario no encontrado");

        if (comment.UserId != user.Id)
            throw new UnauthorizedAccessException("No puedes eliminar comentarios de otros usuarios");

        comment.Delete();
        await _repository.UpdateAsync(comment);
    }
}
