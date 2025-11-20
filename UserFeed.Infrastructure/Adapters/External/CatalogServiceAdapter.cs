using System.Net.Http.Headers;
using UserFeed.Domain.Ports;
using UserFeed.Infrastructure.Configuration;

namespace UserFeed.Infrastructure.Adapters.External;

public class CatalogServiceAdapter : ICatalogService
{
    private readonly HttpClient _httpClient;
    private readonly CatalogServiceSettings _settings;

    public CatalogServiceAdapter(HttpClient httpClient, CatalogServiceSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<bool> ArticleExistsAsync(string articleId)
    {
        var article = await GetArticleAsync(articleId);
        return article != null && article.Enabled;
    }

    public async Task<CatalogArticle?> GetArticleAsync(string articleId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_settings.BaseUrl}/articles/{articleId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var article = System.Text.Json.JsonSerializer.Deserialize<CatalogArticle>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return article;
        }
        catch
        {
            return null;
        }
    }
}
