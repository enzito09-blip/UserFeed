using UserFeed.Domain.Entities;

namespace UserFeed.Domain.Ports;

public interface IUserCommentRepository
{
    Task<UserComment?> GetByIdAsync(string id);
    Task<IEnumerable<UserComment>> GetByArticleIdAsync(string articleId, int page = 1, int pageSize = 10);
    Task<IEnumerable<UserComment>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
    Task<UserComment> CreateAsync(UserComment comment);
    Task<UserComment> UpdateAsync(UserComment comment);
    Task DeleteAsync(string id);
    Task<IEnumerable<(string ArticleId, int CommentCount, double AverageRating)>> GetDistinctArticlesAsync();
}
