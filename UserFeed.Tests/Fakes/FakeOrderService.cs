using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserFeed.Domain.Ports;

namespace UserFeed.Tests.Fakes;

public class FakeOrderService : IOrderService
{
    private readonly Dictionary<string, List<Order>> _userOrders = new();
    private readonly Dictionary<string, HashSet<string>> _userPurchasedArticles = new();

    public Task<bool> UserPurchasedArticleAsync(string userId, string articleId, string token)
    {
        if (_userPurchasedArticles.TryGetValue(userId, out var articles))
        {
            return Task.FromResult(articles.Contains(articleId));
        }
        return Task.FromResult(false);
    }

    public Task<IEnumerable<Order>> GetUserOrdersAsync(string token)
    {
        // Return orders for the current user token (simplified for tests)
        // In real tests, the orders are set via SetUserOrders
        var allOrders = _userOrders.SelectMany(kvp => kvp.Value).ToList();
        return Task.FromResult<IEnumerable<Order>>(allOrders);
    }

    // Test helpers
    public void AddUserOrder(string userId, Order order)
    {
        if (!_userOrders.ContainsKey(userId))
            _userOrders[userId] = new List<Order>();
        _userOrders[userId].Add(order);
    }

    public void AddUserPurchasedArticle(string userId, string articleId)
    {
        if (!_userPurchasedArticles.ContainsKey(userId))
            _userPurchasedArticles[userId] = new HashSet<string>();
        _userPurchasedArticles[userId].Add(articleId);
    }

    public void SetUserOrders(string userId, List<Order> orders)
    {
        _userOrders[userId] = orders;
    }

    public List<Order> GetOrdersForUser(string userId)
    {
        return _userOrders.TryGetValue(userId, out var orders) ? orders : new List<Order>();
    }

    public void Clear()
    {
        _userOrders.Clear();
        _userPurchasedArticles.Clear();
    }
}
