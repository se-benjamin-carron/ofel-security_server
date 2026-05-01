namespace Ofel.Security.Server.Services;

public class SecurityConfig
{
    public string PasswordHash           { get; }
    public string BlacklistPath          { get; }
    public string WhitelistPath          { get; }
    public string TrialPath              { get; }
    public string AlertEmail             { get; }
    public string SmtpHost               { get; }
    public int    SmtpPort               { get; }
    public string SmtpUser               { get; }
    public string SmtpPassword           { get; }
    public int    RateLimitMaxRequests   { get; }
    public int    RateLimitWindowSeconds { get; }
    public int    TimestampToleranceMs   { get; }
    public int    NonceWindowSeconds     { get; }
    public string AdminKey               { get; }

    public SecurityConfig(IConfiguration config)
    {
        PasswordHash           = Require(config, "OFEL_PASSWORD_HASH");
        BlacklistPath          = config["OFEL_BLACKLIST_PATH"]              ?? "/data/blacklist.csv";
        WhitelistPath          = config["OFEL_WHITELIST_PATH"]              ?? "/data/whitelist.csv";
        TrialPath              = config["OFEL_TRIAL_PATH"]                  ?? "/data/trial_list.csv";
        AdminKey               = config["OFEL_ADMIN_KEY"]                   ?? "";
        AlertEmail             = config["OFEL_ALERT_EMAIL"]                 ?? "";
        SmtpHost               = config["OFEL_SMTP_HOST"]                   ?? "smtp.gmail.com";
        SmtpPort               = int.Parse(config["OFEL_SMTP_PORT"]         ?? "587");
        SmtpUser               = config["OFEL_SMTP_USER"]                   ?? "";
        SmtpPassword           = config["OFEL_SMTP_PASSWORD"]               ?? "";
        RateLimitMaxRequests   = int.Parse(config["OFEL_RATE_LIMIT_MAX_REQUESTS"]   ?? "5");
        RateLimitWindowSeconds = int.Parse(config["OFEL_RATE_LIMIT_WINDOW_SECONDS"] ?? "600");
        TimestampToleranceMs   = int.Parse(config["OFEL_TIMESTAMP_TOLERANCE_MS"]    ?? "30000");
        NonceWindowSeconds     = int.Parse(config["OFEL_NONCE_WINDOW_SECONDS"]      ?? "300");

        Console.WriteLine($"[SecurityConfig] Rate limit  : {RateLimitMaxRequests} req / {RateLimitWindowSeconds}s");
        Console.WriteLine($"[SecurityConfig] Timestamp   : ±{TimestampToleranceMs}ms tolerance");
        Console.WriteLine($"[SecurityConfig] Nonce window: {NonceWindowSeconds}s");
        Console.WriteLine($"[SecurityConfig] Blacklist   : {BlacklistPath}");
        Console.WriteLine($"[SecurityConfig] Whitelist   : {WhitelistPath}");
        Console.WriteLine($"[SecurityConfig] Trial list  : {TrialPath}");
    }

    private static string Require(IConfiguration config, string key) =>
        config[key] ?? throw new InvalidOperationException($"Required environment variable '{key}' is not set.");
}
