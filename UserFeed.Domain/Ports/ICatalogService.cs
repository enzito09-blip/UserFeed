namespace UserFeed.Domain.Ports;

public interface ICatalogService
{
    Task<bool> ArticleExistsAsync(string articleId, string? token = null);
    Task<CatalogArticle?> GetArticleAsync(string articleId, string? token = null);
    Task<IEnumerable<CatalogArticle>> GetAllArticlesAsync(string? token = null);
}

public class CatalogArticle
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Stock { get; set; }
    public bool Enabled { get; set; }
}
