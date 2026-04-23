namespace Gateway.Auth;

// ─────────────────────────────────────────────────────────────────────────────
// PasswordHasher — thin BCrypt wrapper
// ─────────────────────────────────────────────────────────────────────────────

public static class PasswordHasher
{
    private const int WorkFactor = 11;

    /// <summary>Returns a BCrypt hash of <paramref name="password"/>.</summary>
    public static string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="password"/> matches
    /// <paramref name="hash"/>.
    /// </summary>
    public static bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
