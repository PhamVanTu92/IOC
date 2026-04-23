namespace Gateway.Auth;

// ─────────────────────────────────────────────────────────────────────────────
// JwtOptions — strongly-typed config bound from "Jwt" section in appsettings
// ─────────────────────────────────────────────────────────────────────────────

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Signing secret — must be at least 32 characters.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Token issuer (iss claim).</summary>
    public string Issuer { get; set; } = "ioc-gateway";

    /// <summary>Token audience (aud claim).</summary>
    public string Audience { get; set; } = "ioc-frontend";

    /// <summary>Access-token lifetime in minutes. Default 480 = 8 h.</summary>
    public int ExpiryMinutes { get; set; } = 480;

    /// <summary>Refresh-token lifetime in days. Default 7.</summary>
    public int RefreshExpiryDays { get; set; } = 7;
}
