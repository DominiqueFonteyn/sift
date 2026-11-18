using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sift.Application.Common;
using Sift.Application.Identity;

namespace Sift.Infrastructure.Identity;

public sealed class JwtTokenIssuer(JwtOptions options, IClock clock) : ITokenIssuer
{
    private readonly JsonWebTokenHandler handler = new();

    public IssuedToken Issue(Guid userId)
    {
        var issuedAt = clock.UtcNow;
        var expiresAt = issuedAt + options.Lifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = userId.ToString() },
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new IssuedToken(handler.CreateToken(descriptor), expiresAt);
    }
}
