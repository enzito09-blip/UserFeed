using UserFeed.Application.DTOs;
using UserFeed.Domain.Entities;
using UserFeed.Domain.Ports;
using System.IdentityModel.Tokens.Jwt;

namespace UserFeed.Application.UseCases;

public class CreateCommentUseCase
{
    private readonly IUserCommentRepository _repository;
    private readonly ICatalogService _catalogService;
    private readonly IOrderService _orderService;

    public CreateCommentUseCase(
        IUserCommentRepository repository, 
        ICatalogService catalogService,
        IOrderService orderService)
    {
        _repository = repository;
        _catalogService = catalogService;
        _orderService = orderService;
    }

    public async Task<CommentResponse> ExecuteAsync(CreateCommentRequest request, string token)
    {
        Console.WriteLine("[CreateComment] Inicio ejecución use case");
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("[CreateComment] Token vacío o nulo");
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }
        Console.WriteLine($"[CreateComment] Token recibido (len={token.Length})");
        var userId = ExtractUserIdFromToken(token!);
        Console.WriteLine($"[CreateComment] userId extraído: {(string.IsNullOrEmpty(userId) ? "<NULL>" : userId)}");
        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("[CreateComment] userId no encontrado en claims");
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }

        // Validar datos básicos
        if (string.IsNullOrWhiteSpace(request.ArticleId))
            throw new ArgumentException("ArticleId es requerido");

        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new ArgumentException("Comment es requerido");

        if (request.Comment.Length > 500)
            throw new ArgumentException("El comentario es demasiado largo, debe tener 500 caracteres o menos");

        if (request.Rating < 1 || request.Rating > 5)
            throw new ArgumentException("Rating debe estar entre 1 y 5");

        // 1. Validar que el usuario tenga órdenes con status "validated"
        Console.WriteLine("[CreateComment] Consultando órdenes del usuario");
        var orders = await _orderService.GetUserOrdersAsync(token);
        var validatedOrders = orders.Where(o => o.Status == "validated").ToList();
        Console.WriteLine($"[CreateComment] Órdenes validadas encontradas: {validatedOrders.Count}");
        if (!validatedOrders.Any())
        {
            Console.WriteLine("[CreateComment] Usuario sin órdenes validadas");
            throw new UnauthorizedAccessException($"El usuario no tiene ordenes de compra validas por lo que no podrá crear un comentario sobre este articulo {request.ArticleId}");
        }

        // 2. Validar que el artículo exista en el catálogo
        Console.WriteLine($"[CreateComment] Verificando existencia de artículo {request.ArticleId}");
        var articleExists = await _catalogService.ArticleExistsAsync(request.ArticleId, token);
        Console.WriteLine($"[CreateComment] Artículo existe: {articleExists}");
        if (!articleExists)
        {
            Console.WriteLine("[CreateComment] Artículo no existe en catálogo");
            throw new ArgumentException($"El articulo seleccionado {request.ArticleId} no existe");
        }

        // 3. Validar que el artículo esté en alguna orden validada del usuario
        Console.WriteLine("[CreateComment] Verificando compra del artículo por el usuario");
        var userPurchased = await _orderService.UserPurchasedArticleAsync(userId, request.ArticleId, token);
        Console.WriteLine($"[CreateComment] Usuario compró artículo: {userPurchased}");
        if (!userPurchased)
        {
            Console.WriteLine("[CreateComment] Artículo no comprado por el usuario");
            throw new UnauthorizedAccessException($"El articulo {request.ArticleId} al cual se quiere comentar no ha sido comprado en ninguna orden por el usuario");
        }

        // 4. Validar que no exista ya un comentario del usuario para este artículo
        Console.WriteLine("[CreateComment] Verificando existencia previa de comentario");
        var existingComment = await _repository.GetByUserAndArticleAsync(userId, request.ArticleId);
        Console.WriteLine($"[CreateComment] Comentario previo existe: {existingComment != null}");
        if (existingComment != null)
        {
            Console.WriteLine("[CreateComment] Comentario duplicado detectado");
            throw new InvalidOperationException($"Ya existe un comentario para el articulo {request.ArticleId} del usuario {userId}");
        }

        // Crear comentario
        Console.WriteLine("[CreateComment] Creando comentario");
        var comment = new UserComment
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            ArticleId = request.ArticleId,
            Comment = request.Comment,
            Rating = request.Rating,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var created = await _repository.CreateAsync(comment);
        Console.WriteLine("[CreateComment] Comentario creado correctamente");

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

    private string? ExtractUserIdFromToken(string authHeader)
    {
        try
        {
            var token = authHeader.Replace("Bearer ", "").Trim();
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimTypes = string.Join(",", jwtToken.Claims.Select(c => c.Type));
            Console.WriteLine($"[CreateComment] Claims presentes: {claimTypes}");
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c =>
                string.Equals(c.Type, "userId", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "userID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "uid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "sub", StringComparison.OrdinalIgnoreCase));
            return userIdClaim?.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateComment] Error leyendo token JWT: {ex.Message}");
            return null;
        }
    }
}

