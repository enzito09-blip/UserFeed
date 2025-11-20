namespace UserFeed.Domain.Ports;

public interface ICatalogService
{
    Task<bool> ArticleExistsAsync(string articleId);
    Task<CatalogArticle?> GetArticleAsync(string articleId);
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
