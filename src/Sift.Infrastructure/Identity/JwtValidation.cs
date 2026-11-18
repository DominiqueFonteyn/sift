using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Sift.Infrastructure.Identity;

public static class JwtValidation
{
    public static TokenValidationParameters CreateParameters(JwtOptions options) => new()
    {
        ValidIssuer = options.Issuer,
        ValidAudience = options.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        // Without refresh tokens a stale token must stop working exactly at its expiry.
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "sub"
    };
}
