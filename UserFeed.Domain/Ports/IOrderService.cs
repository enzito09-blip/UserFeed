namespace UserFeed.Domain.Ports;

public interface IOrderService
{
    Task<bool> UserPurchasedArticleAsync(string userId, string articleId, string token);
    Task<IEnumerable<Order>> GetUserOrdersAsync(string token);
}

public class Order
{
    public string Id { get; set; } = string.Empty;
    public string CartId { get; set; } = string.Empty;
    public int Articles { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public decimal TotalPayment { get; set; }
    public string Created { get; set; } = string.Empty;
    public string Updated { get; set; } = string.Empty;
}
