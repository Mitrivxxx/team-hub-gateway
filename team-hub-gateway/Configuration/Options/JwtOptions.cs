namespace team_hub_gateway.Configuration.Options;

public sealed class JwtOptions
{
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Key) &&
        !string.IsNullOrWhiteSpace(Issuer) &&
        !string.IsNullOrWhiteSpace(Audience);
}
