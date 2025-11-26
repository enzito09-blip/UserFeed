using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserFeed.Domain.Entities;
using UserFeed.Domain.Ports;

namespace UserFeed.Tests.Fakes;

public class FakeUserCommentRepository : IUserCommentRepository
{
    private readonly List<UserComment> _comments = new();

    public Task<UserComment?> GetByIdAsync(string id)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(comment);
    }

    public Task<IEnumerable<UserComment>> GetByArticleIdAsync(string articleId, int page = 1, int pageSize = 10)
    {
        var result = _comments
            .Where(c => c.ArticleId == articleId && !c.IsDeleted)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult<IEnumerable<UserComment>>(result);
    }

    public Task<IEnumerable<UserComment>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
    {
        var result = _comments
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult<IEnumerable<UserComment>>(result);
    }

    public Task<UserComment?> GetByUserAndArticleAsync(string userId, string articleId)
    {
        var comment = _comments.FirstOrDefault(c =>
            c.UserId == userId && c.ArticleId == articleId && !c.IsDeleted);
        return Task.FromResult(comment);
    }

    public Task<UserComment> CreateAsync(UserComment comment)
    {
        _comments.Add(comment);
        return Task.FromResult(comment);
    }

    public Task<UserComment> UpdateAsync(UserComment comment)
    {
        var existing = _comments.FirstOrDefault(c => c.Id == comment.Id);
        if (existing != null)
        {
            _comments.Remove(existing);
            _comments.Add(comment);
        }
        return Task.FromResult(comment);
    }

    public Task DeleteAsync(string id)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == id);
        if (comment != null)
        {
            _comments.Remove(comment);
        }
        return Task.CompletedTask;
    }

    // Helper methods for tests
    public void AddComment(UserComment comment)
    {
        _comments.Add(comment);
    }

    public void Clear()
    {
        _comments.Clear();
    }

    public IEnumerable<UserComment> GetAll()
    {
        return _comments;
    }
}
