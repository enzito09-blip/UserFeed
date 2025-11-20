using UserFeed.Application.DTOs;
using UserFeed.Domain.Entities;
using UserFeed.Domain.Ports;

namespace UserFeed.Application.UseCases;

public class CreateCommentUseCase
{
    private readonly IUserCommentRepository _repository;
    private readonly IAuthService _authService;
    private readonly ICatalogService _catalogService;
    private readonly IOrderService _orderService;

    public CreateCommentUseCase(
        IUserCommentRepository repository, 
        IAuthService authService,
        ICatalogService catalogService,
        IOrderService orderService)
    {
        _repository = repository;
        _authService = authService;
        _catalogService = catalogService;
        _orderService = orderService;
    }

    public async Task<CommentResponse> ExecuteAsync(CreateCommentRequest request, string token)
    {
        // Validar usuario autenticado
        var user = await _authService.GetCurrentUserAsync(token);
        if (user == null)
            throw new UnauthorizedAccessException("Usuario no autenticado");

        // Validar datos
        if (string.IsNullOrWhiteSpace(request.ArticleId))
            throw new ArgumentException("ArticleId es requerido");

        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new ArgumentException("Comment es requerido");

        if (request.Rating < 1 || request.Rating > 5)
            throw new ArgumentException("Rating debe estar entre 1 y 5");

        // Validar que el artículo exista en el catálogo
        var articleExists = await _catalogService.ArticleExistsAsync(request.ArticleId);
        if (!articleExists)
            throw new ArgumentException($"El artículo {request.ArticleId} no existe en el catálogo");

        // Validar que el usuario haya comprado el artículo
        var userPurchased = await _orderService.UserPurchasedArticleAsync(user.Id, request.ArticleId);
        if (!userPurchased)
            throw new UnauthorizedAccessException("Solo puedes comentar artículos que hayas comprado");

        // Crear comentario
        var comment = new UserComment
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ArticleId = request.ArticleId,
            Comment = request.Comment,
            Rating = request.Rating,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var created = await _repository.CreateAsync(comment);

        return new CommentResponse
        {
            Id = created.Id,
            UserId = created.UserId,
            ArticleId = created.ArticleId,
            Comment = created.Comment,
            Rating = created.Rating,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };
    }
}
