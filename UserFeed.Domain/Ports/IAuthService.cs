using UserFeed.Domain.Entities;

namespace UserFeed.Domain.Ports;

public interface IAuthService
{
    Task<User?> GetCurrentUserAsync(string token);
    void InvalidateTokenCache(string token);
}
