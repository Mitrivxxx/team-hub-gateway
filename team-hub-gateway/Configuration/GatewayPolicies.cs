namespace team_hub_gateway.Configuration;

internal static class GatewayPolicies
{
    public const string Cors = "GatewayCorsPolicy";
    public const string RateLimit = "GatewayIpPolicy";
    public const string InviteCreate = "InviteCreatePolicy";
    public const string InviteAccept = "InviteAcceptPolicy";
    public const string Import = "ImportPolicy";
    public const string Avatar = "AvatarPolicy";
}
