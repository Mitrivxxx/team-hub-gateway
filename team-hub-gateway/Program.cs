using team_hub_gateway.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("reverseproxy.json", optional: false, reloadOnChange: true);
builder.Services.AddGatewayInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseGatewayPipeline();
app.MapHealthChecks("/healtch");

app.Run();
