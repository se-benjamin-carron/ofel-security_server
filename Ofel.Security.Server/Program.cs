using Ofel.Security.Server.Endpoints;
using Ofel.Security.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SecurityConfig>();
builder.Services.AddSingleton<RateLimiterService>();
builder.Services.AddSingleton<NonceService>();
builder.Services.AddSingleton<BlacklistService>();
builder.Services.AddSingleton<EmailAlertService>();

var app = builder.Build();

// Force eager initialisation so startup errors are visible immediately.
app.Services.GetRequiredService<SecurityConfig>();
app.Services.GetRequiredService<BlacklistService>();

VerifyEndpoint.Map(app);
AlertEndpoint.Map(app);

app.Run();
