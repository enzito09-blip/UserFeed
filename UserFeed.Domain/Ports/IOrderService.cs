namespace UserFeed.Domain.Ports;

public interface IOrderService
{
    Task<bool> UserPurchasedArticleAsync(string userId, string articleId);
}
