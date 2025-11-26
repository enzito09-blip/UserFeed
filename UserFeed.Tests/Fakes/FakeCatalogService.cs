using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserFeed.Domain.Ports;

namespace UserFeed.Tests.Fakes;

public class FakeCatalogService : ICatalogService
{
    private readonly List<string> _existingArticles = new();

    public Task<bool> ArticleExistsAsync(string articleId, string? token = null)
    {
        return Task.FromResult(_existingArticles.Contains(articleId));
    }

    public Task<CatalogArticle?> GetArticleAsync(string articleId, string? token = null)
    {
        if (!_existingArticles.Contains(articleId))
            return Task.FromResult<CatalogArticle?>(null);

        return Task.FromResult<CatalogArticle?>(new CatalogArticle
        {
            Id = articleId,
            Name = $"Article {articleId}",
            Description = "Test article",
            Image = "test.jpg",
            Price = 100.0,
            Stock = 10,
            Enabled = true
        });
    }

    public Task<IEnumerable<CatalogArticle>> GetAllArticlesAsync(string? token = null)
    {
        var articles = _existingArticles.Select(id => new CatalogArticle
        {
            Id = id,
            Name = $"Article {id}",
            Description = "Test article",
            Image = "test.jpg",
            Price = 100.0,
            Stock = 10,
            Enabled = true
        });
        return Task.FromResult<IEnumerable<CatalogArticle>>(articles);
    }

    // Test helpers
    public void AddArticle(string articleId)
    {
        if (!_existingArticles.Contains(articleId))
            _existingArticles.Add(articleId);
    }

    public void Clear()
    {
        _existingArticles.Clear();
    }
}
