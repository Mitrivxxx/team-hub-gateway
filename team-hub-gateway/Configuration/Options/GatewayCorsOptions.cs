namespace team_hub_gateway.Configuration.Options;

public sealed class GatewayCorsOptions
{
    public string[] AllowedOrigins { get; init; } = [];
}
