using UserFeed.Domain.Entities;

namespace UserFeed.Domain.Interfaces;

public interface IAuthService
{
    Task<User?> GetCurrentUserAsync(string token);
    void InvalidateTokenCache(string token);
}
