using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using team_hub_gateway.Configuration;
using Xunit;

namespace tesam_hub_gateway.Test;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGatewayInfrastructure_Throws_WhenAuthEnabledAndJwtMissing()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AuthValidation:Enabled"] = "true",
            ["ReverseProxy:Routes:auth:ClusterId"] = "auth-cluster",
            ["ReverseProxy:Clusters:auth-cluster:Destinations:d1:Address"] = "http://localhost:5001/"
        });

        var action = () => services.AddGatewayInfrastructure(configuration);

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("AuthValidation is enabled but Jwt:Key/Issuer/Audience are missing.", exception.Message);
    }

    [Fact]
    public void AddGatewayInfrastructure_DoesNotThrow_WhenAuthDisabledAndJwtMissing()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AuthValidation:Enabled"] = "false",
            ["ReverseProxy:Routes:auth:ClusterId"] = "auth-cluster",
            ["ReverseProxy:Clusters:auth-cluster:Destinations:d1:Address"] = "http://localhost:5001/"
        });

        services.AddGatewayInfrastructure(configuration);
    }

    [Fact]
    public async Task AddGatewayInfrastructure_RegistersJwtBearerScheme_WhenJwtConfigured()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AuthValidation:Enabled"] = "true",
            ["Jwt:Key"] = "01234567890123456789012345678901",
            ["Jwt:Issuer"] = "team-hub",
            ["Jwt:Audience"] = "team-hub-web",
            ["ReverseProxy:Routes:auth:ClusterId"] = "auth-cluster",
            ["ReverseProxy:Clusters:auth-cluster:Destinations:d1:Address"] = "http://localhost:5001/"
        });

        services.AddGatewayInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme);

        Assert.NotNull(scheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, scheme!.Name);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
