namespace team_hub_gateway.Configuration.Options;

public sealed class RateLimitingOptions
{
    public int PermitLimit { get; init; } = 100;
    public int WindowSeconds { get; init; } = 60;
    public int QueueLimit { get; init; } = 0;

    public int InvitePermitLimit { get; init; } = 5;
    public int InviteAcceptPermitLimit { get; init; } = 10;
    public int ImportPermitLimit { get; init; } = 3;
    public int AvatarPermitLimit { get; init; } = 10;
    public int SensitiveWindowSeconds { get; init; } = 60;
}
