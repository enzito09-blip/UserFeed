using System.Net.Http.Headers;
using System.Text.Json;
using UserFeed.Domain.Ports;
using UserFeed.Infrastructure.Configuration;

namespace UserFeed.Infrastructure.Adapters.External;

public class OrderServiceAdapter : IOrderService
{
    private readonly HttpClient _httpClient;
    private readonly OrderServiceSettings _settings;

    public OrderServiceAdapter(HttpClient httpClient, OrderServiceSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<bool> UserPurchasedArticleAsync(string userId, string articleId)
    {
        try
        {
            // Consultar las órdenes del usuario
            var response = await _httpClient.GetAsync($"{_settings.BaseUrl}/v1/orders");
            
            if (!response.IsSuccessStatusCode)
                return false;

            var content = await response.Content.ReadAsStringAsync();
            var orders = JsonSerializer.Deserialize<List<OrderResponse>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (orders == null)
                return false;

            // Verificar si alguna orden contiene el artículo
            return orders.Any(order => 
                order.Articles != null && 
                order.Articles.Any(a => a.ArticleId == articleId));
        }
        catch
        {
            return false;
        }
    }

    private class OrderResponse
    {
        public string? Id { get; set; }
        public List<OrderArticle>? Articles { get; set; }
    }

    private class OrderArticle
    {
        public string? ArticleId { get; set; }
        public int Quantity { get; set; }
    }
}
