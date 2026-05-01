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

// CORS — allow the local admin UI to call /admin/* endpoints.
var corsOrigins = (builder.Configuration["OFEL_ADMIN_CORS_ORIGINS"]
    ?? "http://localhost:5173,http://127.0.0.1:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminCors", policy =>
        policy.WithOrigins(corsOrigins)
              .WithHeaders("X-Admin-Key", "Content-Type")
              .WithMethods("GET", "POST", "DELETE", "OPTIONS"));
});

builder.Services.AddSingleton<SecurityConfig>();
builder.Services.AddSingleton<RateLimiterService>();
builder.Services.AddSingleton<NonceService>();
builder.Services.AddSingleton<BlacklistService>();
builder.Services.AddSingleton<WhitelistService>();
builder.Services.AddSingleton<TrustedService>();
builder.Services.AddSingleton<TrialService>();
builder.Services.AddSingleton<EmailAlertService>();

var app = builder.Build();

// Force eager initialisation so startup errors are visible immediately.
app.Services.GetRequiredService<SecurityConfig>();
app.Services.GetRequiredService<BlacklistService>();
app.Services.GetRequiredService<TrustedService>();
app.Services.GetRequiredService<TrialService>();

app.UseCors();

VerifyEndpoint.Map(app);
AlertEndpoint.Map(app);
AdminEndpoint.Map(app);

app.Run();
