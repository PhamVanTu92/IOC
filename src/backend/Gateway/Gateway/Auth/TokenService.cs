using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Auth;

// ─────────────────────────────────────────────────────────────────────────────
// TokenService — generates signed JWT access tokens
// ─────────────────────────────────────────────────────────────────────────────

public sealed class TokenService(JwtOptions options)
{
    private readonly JwtSecurityTokenHandler _handler = new();

    /// <summary>
    /// Creates a signed JWT for the given user.
    /// Claims included: sub, email, tid, role, iss, aud, iat, exp.
    /// </summary>
    public string GenerateAccessToken(
        Guid userId,
        Guid tenantId,
        string email,
        string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("tid",                          tenantId.ToString()),
            new Claim(ClaimTypes.Role,               role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var now      = DateTime.UtcNow;
        var expires  = now.AddMinutes(options.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer:             options.Issuer,
            audience:           options.Audience,
            claims:             claims,
            notBefore:          now,
            expires:            expires,
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }

    /// <summary>Returns the expiry instant for the next access token.</summary>
    public DateTime GetExpiresAt() =>
        DateTime.UtcNow.AddMinutes(options.ExpiryMinutes);
}
