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
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
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
            options.AddPolicy(GatewayPolicies.RateLimit, context =>
            {
                var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
                        QueueLimit = rateLimitingOptions.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });

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

        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }
}
