using UserFeed.Application.DTOs;
using UserFeed.Domain.Ports;

namespace UserFeed.Application.UseCases;

public class GetArticlesWithCommentsUseCase
{
    private readonly IUserCommentRepository _repository;
    private readonly ICatalogService _catalogService;

    public GetArticlesWithCommentsUseCase(IUserCommentRepository repository, ICatalogService catalogService)
    {
        _repository = repository;
        _catalogService = catalogService;
    }

    public async Task<IEnumerable<ArticleInfoResponse>> ExecuteAsync()
    {
        var articles = await _repository.GetDistinctArticlesAsync();
        var result = new List<ArticleInfoResponse>();

        foreach (var a in articles)
        {
            var catalogArticle = await _catalogService.GetArticleAsync(a.ArticleId);

            // Solo incluir si el artículo existe en catálogo
            if (catalogArticle != null)
            {
                result.Add(new ArticleInfoResponse
                {
                    ArticleId = a.ArticleId,
                    Name = catalogArticle.Name,
                    Description = catalogArticle.Description,
                    Image = catalogArticle.Image,
                    Price = catalogArticle.Price,
                    Stock = catalogArticle.Stock,
                    Enabled = catalogArticle.Enabled,
                    CommentCount = a.CommentCount,
                    AverageRating = Math.Round(a.AverageRating, 2)
                });
            }
        }

        return result;
    }
}
