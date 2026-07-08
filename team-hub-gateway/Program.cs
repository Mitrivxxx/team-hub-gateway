using team_hub_gateway.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("reverseproxy.json", optional: false, reloadOnChange: true);
builder.Services.AddGatewayInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseGatewayPipeline();

app.Run();
