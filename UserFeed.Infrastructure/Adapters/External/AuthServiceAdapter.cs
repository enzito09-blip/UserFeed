using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using UserFeed.Domain.Entities;
using UserFeed.Domain.Ports;
using UserFeed.Infrastructure.Configuration;

namespace UserFeed.Infrastructure.Adapters.External;

public class AuthServiceAdapter : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthServiceSettings _settings;
    private readonly ConcurrentDictionary<string, User> _tokenCache;

    public AuthServiceAdapter(HttpClient httpClient, AuthServiceSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
        _tokenCache = new ConcurrentDictionary<string, User>();
    }

    public async Task<User?> GetCurrentUserAsync(string token)
    {
        // Verificar cache
        if (_tokenCache.TryGetValue(token, out var cachedUser))
        {
            return cachedUser;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{_settings.BaseUrl}{_settings.CurrentUserEndpoint}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<User>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (user != null)
            {
                // Guardar en cache
                _tokenCache.TryAdd(token, user);
            }

            return user;
        }
        catch
        {
            return null;
        }
    }

    public void InvalidateTokenCache(string token)
    {
        _tokenCache.TryRemove(token, out _);
    }
}
