using UserFeed.Application.DTOs;
using UserFeed.Domain.Ports;

namespace UserFeed.Application.UseCases;

public class GetCommentsByArticleUseCase
{
    private readonly IUserCommentRepository _repository;

    public GetCommentsByArticleUseCase(IUserCommentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CommentResponse>> ExecuteAsync(string articleId, int page = 1, int pageSize = 10)
    {
        var comments = await _repository.GetByArticleIdAsync(articleId, page, pageSize);

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
}
