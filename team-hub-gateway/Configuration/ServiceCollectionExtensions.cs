using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using team_hub_gateway.Configuration.Options;
using System.Threading.RateLimiting;

namespace team_hub_gateway.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<GatewayCorsOptions>()
            .Bind(configuration.GetSection("Cors"))
            .ValidateOnStart();

        services
            .AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection("RateLimiting"))
            .ValidateOnStart();

        services
            .AddOptions<AuthValidationOptions>()
            .Bind(configuration.GetSection("AuthValidation"))
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateOnStart();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var corsOptions = configuration.GetSection("Cors").Get<GatewayCorsOptions>() ?? new GatewayCorsOptions();
        services.AddCors(options =>
        {
            options.AddPolicy(GatewayPolicies.Cors, policy =>
            {
                if (corsOptions.AllowedOrigins is { Length: > 0 })
                {
                    policy.WithOrigins(corsOptions.AllowedOrigins);
                }

                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        var rateLimitingOptions = configuration.GetSection("RateLimiting").Get<RateLimitingOptions>() ?? new RateLimitingOptions();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            FixedWindowRateLimiterOptions Fixed(int permitLimit, int windowSeconds) => new()
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = rateLimitingOptions.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            };

            options.AddPolicy(GatewayPolicies.RateLimit, context =>
            {
                var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => Fixed(rateLimitingOptions.PermitLimit, rateLimitingOptions.WindowSeconds));
            });

            options.AddPolicy(GatewayPolicies.InviteCreate, context =>
            {
                var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"invite-create:{key}",
                    factory: _ => Fixed(rateLimitingOptions.InvitePermitLimit, rateLimitingOptions.SensitiveWindowSeconds));
            });

            options.AddPolicy(GatewayPolicies.InviteAccept, context =>
            {
                var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"invite-accept:{key}",
                    factory: _ => Fixed(rateLimitingOptions.InviteAcceptPermitLimit, rateLimitingOptions.SensitiveWindowSeconds));
            });

            options.AddPolicy(GatewayPolicies.Import, context =>
            {
                var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"import:{key}",
                    factory: _ => Fixed(rateLimitingOptions.ImportPermitLimit, rateLimitingOptions.SensitiveWindowSeconds));
            });

            options.AddPolicy(GatewayPolicies.Avatar, context =>
            {
                var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"avatar:{key}",
                    factory: _ => Fixed(rateLimitingOptions.AvatarPermitLimit, rateLimitingOptions.SensitiveWindowSeconds));
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var policy = ResolveSensitivePolicy(context, rateLimitingOptions);
                if (policy is null)
                    return RateLimitPartition.GetNoLimiter("none");

                var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var (name, limit) = policy.Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"{name}:{key}",
                    _ => Fixed(limit, rateLimitingOptions.SensitiveWindowSeconds));
            });
        });

        static (string Name, int Limit)? ResolveSensitivePolicy(HttpContext context, RateLimitingOptions rateLimitingOptions)
        {
            var path = context.Request.Path.Value ?? "";
            var method = context.Request.Method;

            if (HttpMethods.IsPost(method)
                && path.Contains("/invitations/by-token/", StringComparison.OrdinalIgnoreCase)
                && path.EndsWith("/accept", StringComparison.OrdinalIgnoreCase))
                return (GatewayPolicies.InviteAccept, rateLimitingOptions.InviteAcceptPermitLimit);

            if (HttpMethods.IsPost(method)
                && path.Contains("/invitations", StringComparison.OrdinalIgnoreCase)
                && (path.EndsWith("/invitations", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith("/invitations/", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith("/resend", StringComparison.OrdinalIgnoreCase)))
                return (GatewayPolicies.InviteCreate, rateLimitingOptions.InvitePermitLimit);

            if (HttpMethods.IsPost(method)
                && path.Contains("/imports", StringComparison.OrdinalIgnoreCase))
                return (GatewayPolicies.Import, rateLimitingOptions.ImportPermitLimit);

            if (HttpMethods.IsPut(method)
                && path.EndsWith("/avatar", StringComparison.OrdinalIgnoreCase))
                return (GatewayPolicies.Avatar, rateLimitingOptions.AvatarPermitLimit);

            return null;
        }

        var authValidationOptions = configuration.GetSection("AuthValidation").Get<AuthValidationOptions>() ?? new AuthValidationOptions();
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (authValidationOptions.Enabled && !jwtOptions.IsConfigured)
        {
            throw new InvalidOperationException("AuthValidation is enabled but Jwt:Key/Issuer/Audience are missing.");
        }

        if (jwtOptions.IsConfigured)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
                    };
                });

            services.AddAuthorization();
        }

        services.AddServiceDiscovery();

        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver();

        return services;
    }
}
