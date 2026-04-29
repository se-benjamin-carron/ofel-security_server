using System.Text.Json;
using Ofel.Security.Server.Endpoints;
using Ofel.Security.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Accept snake_case JSON (machine_id → MachineId, etc.) from clients.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddSingleton<SecurityConfig>();
builder.Services.AddSingleton<RateLimiterService>();
builder.Services.AddSingleton<NonceService>();
builder.Services.AddSingleton<BlacklistService>();
builder.Services.AddSingleton<WhitelistService>();
builder.Services.AddSingleton<EmailAlertService>();

var app = builder.Build();

// Force eager initialisation so startup errors are visible immediately.
app.Services.GetRequiredService<SecurityConfig>();
app.Services.GetRequiredService<BlacklistService>();

VerifyEndpoint.Map(app);
AlertEndpoint.Map(app);
AdminEndpoint.Map(app);

app.Run();
