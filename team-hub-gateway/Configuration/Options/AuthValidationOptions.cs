namespace team_hub_gateway.Configuration.Options;

public sealed class AuthValidationOptions
{
    public bool Enabled { get; init; }

    public string[] ExcludedPathPrefixes { get; init; } =
    [
        "/api/auth/v1/login",
        "/api/auth/v1/register",
        "/api/auth/v1/refresh",
        "/api/auth/v1/logout",
        "/api/auth/v1/change-password"
    ];
}
