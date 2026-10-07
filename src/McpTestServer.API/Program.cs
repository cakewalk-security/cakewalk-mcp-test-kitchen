using McpTestServer.API.Constants;
using McpTestServer.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = McpObservationConstants.MaxRequestBodyBytes;
});

builder.Services.AddHostServices(builder.Configuration);

var app = builder.Build();

app.UseProxyForwardedHeaders();
app.UseSecurityHeaders();
app.UseRequestLogging();
app.UseMcpPatAuth();
app.UseMiddlewares();

await app.RunAsync();
