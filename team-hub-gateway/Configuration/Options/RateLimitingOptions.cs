namespace team_hub_gateway.Configuration.Options;

public sealed class RateLimitingOptions
{
    public int PermitLimit { get; init; } = 100;
    public int WindowSeconds { get; init; } = 60;
    public int QueueLimit { get; init; } = 0;
}
