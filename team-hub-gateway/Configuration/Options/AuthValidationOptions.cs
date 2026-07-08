namespace team_hub_gateway.Configuration.Options;

public sealed class AuthValidationOptions
{
    public bool Enabled { get; init; }

    public string[] ExcludedPathPrefixes { get; init; } =
    [
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/logout"
    ];
}
