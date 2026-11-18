using Microsoft.IdentityModel.JsonWebTokens;
using Sift.Application.Common;
using Sift.Infrastructure.Identity;

namespace Sift.Api.IntegrationTests.Identity;

public sealed class JwtTokenIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        SigningKey = "unit-tests-only-signing-key-0123456789",
        Lifetime = TimeSpan.FromMinutes(30)
    };

    [Fact]
    public void Token_contains_the_user_id_and_an_expiry_one_lifetime_after_issue()
    {
        var userId = Guid.NewGuid();

        var issued = new JwtTokenIssuer(Options, new FixedClock(Now)).Issue(userId);
        var token = new JsonWebTokenHandler().ReadJsonWebToken(issued.Value);

        Assert.Equal(userId.ToString(), token.Subject);
        Assert.Equal(Now.AddMinutes(30), issued.ExpiresAt);
        Assert.Equal(Now.AddMinutes(30).UtcDateTime, token.ValidTo);
        Assert.Equal(Now.UtcDateTime, token.ValidFrom);
    }

    [Fact]
    public void Token_never_carries_the_email_or_any_password_material()
    {
        var issued = new JwtTokenIssuer(Options, new FixedClock(Now)).Issue(Guid.NewGuid());
        var token = new JsonWebTokenHandler().ReadJsonWebToken(issued.Value);

        Assert.DoesNotContain(token.Claims, claim => claim.Type is "email" or "password" or "pwd");
    }

    [Fact]
    public async Task Token_validates_before_expiry_and_fails_after()
    {
        var issued = new JwtTokenIssuer(Options, new FixedClock(DateTimeOffset.UtcNow.AddMinutes(-29))).Issue(Guid.NewGuid());
        var expired = new JwtTokenIssuer(Options, new FixedClock(DateTimeOffset.UtcNow.AddMinutes(-31))).Issue(Guid.NewGuid());
        var parameters = JwtValidation.CreateParameters(Options);
        var handler = new JsonWebTokenHandler();

        Assert.True((await handler.ValidateTokenAsync(issued.Value, parameters)).IsValid);
        Assert.False((await handler.ValidateTokenAsync(expired.Value, parameters)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    public void Rejects_a_missing_or_short_signing_key(string key)
    {
        Assert.Throws<InvalidOperationException>(() => (Options with { SigningKey = key }).EnsureValid());
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
