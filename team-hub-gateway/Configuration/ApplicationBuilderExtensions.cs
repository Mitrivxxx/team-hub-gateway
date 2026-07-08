using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using team_hub_gateway.Configuration.Options;

namespace team_hub_gateway.Configuration;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseGatewayPipeline(this WebApplication app)
    {
        var authValidationOptions = app.Services.GetRequiredService<IOptions<AuthValidationOptions>>().Value;
        var jwtOptions = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var hasJwtConfig = jwtOptions.IsConfigured;

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Internal Server Error" });
            });
        });

        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseCors(GatewayPolicies.Cors);
        app.UseRateLimiter();

        if (hasJwtConfig)
        {
            app.UseAuthentication();
        }

        app.Use(async (context, next) =>
        {
            if (!authValidationOptions.Enabled || !hasJwtConfig)
            {
                await next();
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;
            if (authValidationOptions.ExcludedPathPrefixes.Any(prefix =>
                    path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                await next();
                return;
            }

            var authResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (authResult.Succeeded)
            {
                await next();
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        });

        app.MapReverseProxy();

        return app;
    }
}
