namespace UserFeed.Infrastructure.Configuration;

public class AuthServiceSettings
{
    public string BaseUrl { get; set; } = "http://localhost:3000";
    public string CurrentUserEndpoint { get; set; } = "/v1/users/current";
}
