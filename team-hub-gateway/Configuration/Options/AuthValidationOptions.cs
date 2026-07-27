namespace team_hub_gateway.Configuration.Options;

public sealed class AuthValidationOptions
{
    public bool Enabled { get; init; }

    public string[] ExcludedPathPrefixes { get; init; } =
    [
        "/api/auth/v0.0/login",
        "/api/auth/v0.0/register",
        "/api/auth/v0.0/refresh",
        "/api/auth/v0.0/logout",
        "/api/auth/v0.0/change-password"
    ];
}
