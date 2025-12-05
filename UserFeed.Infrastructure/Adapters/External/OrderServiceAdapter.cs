using System.Net.Http.Headers;
using System.Text.Json;
using UserFeed.Domain.Interfaces;
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

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/orders");
            
            if (!string.IsNullOrEmpty(token))
            {
                var header = token.StartsWith("Bearer ") ? token : $"Bearer {token}";
                request.Headers.Add("Authorization", header);
                Console.WriteLine($"[OrderServiceAdapter] Enviando Authorization: {header.Substring(0, Math.Min(header.Length, 30))}...");
            }

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<Order>();

            var content = await response.Content.ReadAsStringAsync();
            var orders = JsonSerializer.Deserialize<List<Order>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return orders ?? Enumerable.Empty<Order>();
        }
        catch
        {
            return Enumerable.Empty<Order>();
        }
    }

    public async Task<bool> UserPurchasedArticleAsync(string userId, string articleId, string token)
    {
        try
        {
            // 1. Obtener listado resumen de órdenes
            var summaryReq = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/orders");
            if (!string.IsNullOrEmpty(token))
            {
                var header = token.StartsWith("Bearer ") ? token : $"Bearer {token}";
                summaryReq.Headers.Add("Authorization", header);
            }
            var summaryResp = await _httpClient.SendAsync(summaryReq);
            if (!summaryResp.IsSuccessStatusCode)
            {
                Console.WriteLine("[OrderServiceAdapter] No se pudo obtener listado de órdenes");
                return false;
            }
            var summaryJson = await summaryResp.Content.ReadAsStringAsync();
            var summaryOrders = JsonSerializer.Deserialize<List<OrderSummary>>(summaryJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<OrderSummary>();

            var validated = summaryOrders.Where(o => o.Status == "validated").ToList();
            Console.WriteLine($"[OrderServiceAdapter] Órdenes validadas: {validated.Count}");
            if (!validated.Any()) return false;

            // 2. Revisar detalle de cada orden validada
            foreach (var ord in validated)
            {
                var detailReq = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/orders/{ord.Id}");
                if (!string.IsNullOrEmpty(token))
                {
                    var header = token.StartsWith("Bearer ") ? token : $"Bearer {token}";
                    detailReq.Headers.Add("Authorization", header);
                }
                var detailResp = await _httpClient.SendAsync(detailReq);
                if (!detailResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[OrderServiceAdapter] Orden {ord.Id} detalle HTTP {(int)detailResp.StatusCode}");
                    continue;
                }
                var detailJson = await detailResp.Content.ReadAsStringAsync();
                var detail = JsonSerializer.Deserialize<OrderDetail>(detailJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (detail?.Articles == null)
                {
                    Console.WriteLine($"[OrderServiceAdapter] Orden {ord.Id} sin artículos");
                    continue;
                }
                foreach (var art in detail.Articles)
                {
                    Console.WriteLine($"[OrderServiceAdapter] Revisando artículo {art.ArticleId} (isValidated={art.IsValidated})");
                    if (string.Equals(art.ArticleId, articleId, StringComparison.OrdinalIgnoreCase) && art.IsValidated)
                    {
                        Console.WriteLine($"[OrderServiceAdapter] Match artículo comprado {articleId} en orden {ord.Id}");
                        return true;
                    }
                }
            }
            Console.WriteLine($"[OrderServiceAdapter] Artículo {articleId} no encontrado en órdenes validadas");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrderServiceAdapter] Error verificando artículo comprado: {ex.Message}");
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

    private class OrderSummary
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
    }

    private class OrderDetail
    {
        public string? Id { get; set; }
        public List<OrderArticleDetail>? Articles { get; set; }
    }

    private class OrderArticleDetail
    {
        public string? ArticleId { get; set; }
        public int Quantity { get; set; }
        public bool IsValid { get; set; }
        public decimal UnitaryPrice { get; set; }
        public bool IsValidated { get; set; }
    }
}
