using System.Net.Http.Headers;
using UserFeed.Domain.Interfaces;
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

    public async Task<bool> ArticleExistsAsync(string articleId, string? token = null)
    {
        var article = await GetArticleAsync(articleId, token);
        return article != null && article.Enabled;
    }

    public async Task<CatalogArticle?> GetArticleAsync(string articleId, string? token = null)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/articles/{articleId}");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            var response = await _httpClient.SendAsync(request);
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

    public async Task<IEnumerable<CatalogArticle>> GetAllArticlesAsync(string? token = null)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/articles");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new List<CatalogArticle>();

            var json = await response.Content.ReadAsStringAsync();
            var articles = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<CatalogArticle>>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return articles ?? new List<CatalogArticle>();
        }
        catch
        {
            return new List<CatalogArticle>();
        }
    }
}
