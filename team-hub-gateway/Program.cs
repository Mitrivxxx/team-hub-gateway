using team_hub_gateway.Configuration;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("reverseproxy.json", optional: false, reloadOnChange: true);
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddGatewayInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Team Hub Gateway API",
        Version = "v1",
        Description = "Gateway technical endpoints and reverse-proxy routes documentation."
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseGatewayPipeline();
app.MapGet("/health", async (HealthCheckService healthCheckService, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    var report = await healthCheckService.CheckHealthAsync(cancellationToken);
    var payload = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.Status.ToString())
    };

    var statusCode = report.Status == HealthStatus.Healthy
        ? StatusCodes.Status200OK
        : StatusCodes.Status503ServiceUnavailable;

    if (report.Status == HealthStatus.Healthy)
    {
        logger.LogInformation("Health check passed with status {HealthStatus}.", report.Status);
    }
    else
    {
        var failedChecks = report.Entries
            .Where(entry => entry.Value.Status != HealthStatus.Healthy)
            .Select(entry => entry.Key)
            .ToArray();
        logger.LogWarning(
            "Health check returned status {HealthStatus}. Failed checks: {FailedChecks}.",
            report.Status,
            failedChecks);
    }

    return Results.Json(payload, statusCode: statusCode);
})
    .WithName("HealthCheck")
    .WithTags("Observability")
    .WithSummary("Checks gateway health")
    .WithDescription("Runs application health checks and returns overall status with per-check details.");
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();
