var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("reverseproxy.json", optional: false, reloadOnChange: true);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseHttpsRedirection();
app.MapReverseProxy();

app.Run();
