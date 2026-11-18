using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Sift.Api.IntegrationTests.Identity;

// FR-1.3 asks for a valid token, not just a bearer header: each case keeps a real user id so only the token is at fault.
public sealed class InvalidTokenTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    public static TheoryData<string> Cases =>
    [
        "signed-with-another-key",
        "expired",
        "wrong-issuer",
        "wrong-audience",
        "unsigned",
        "tampered-payload",
        "garbage"
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task An_invalid_token_gets_a_401_problem_on_protected_routes(string kind)
    {
        using var signedIn = await AuthenticatedClient.CreateSignedInAsync(factory);
        var userId = await AuthenticatedClient.GetUserIdAsync(signedIn);
        var validToken = signedIn.DefaultRequestHeaders.Authorization!.Parameter!;
        var token = CreateToken(kind, userId, validToken);

        foreach (var path in new[] { "/bureaus", "/auth/me" })
        {
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(path);

            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"{kind} on {path} returned {(int)response.StatusCode}.");
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task The_valid_token_used_as_a_baseline_is_accepted()
    {
        using var signedIn = await AuthenticatedClient.CreateSignedInAsync(factory);
        var userId = await AuthenticatedClient.GetUserIdAsync(signedIn);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Sign(userId));

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string CreateToken(string kind, Guid userId, string validToken) => kind switch
    {
        "signed-with-another-key" => Sign(userId, signingKey: "a-completely-different-signing-key-0123456789"),
        "expired" => Sign(userId, issuedAt: DateTime.UtcNow.AddHours(-2), expires: DateTime.UtcNow.AddMinutes(-1)),
        "wrong-issuer" => Sign(userId, issuer: "someone-else"),
        "wrong-audience" => Sign(userId, audience: "someone-else"),
        "unsigned" => Sign(userId, signingKey: null),
        "tampered-payload" => TamperPayload(validToken, Guid.NewGuid()),
        "garbage" => "not-a-jwt",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static string Sign(
        Guid userId,
        string? signingKey = SiftApiFactory.TestSigningKey,
        string issuer = "sift",
        string audience = "sift",
        DateTime? issuedAt = null,
        DateTime? expires = null)
    {
        var now = issuedAt ?? DateTime.UtcNow;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = userId.ToString() },
            IssuedAt = now,
            NotBefore = now,
            Expires = expires ?? now.AddHours(1),
            SigningCredentials = signingKey is null
                ? null
                : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)
        });
    }

    // Keeps the original header and signature but swaps the subject, as an attacker editing a captured token would.
    private static string TamperPayload(string token, Guid otherUserId)
    {
        var parts = token.Split('.');
        var payload = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1]));
        var original = new JsonWebToken(token).Subject;
        parts[1] = Base64UrlEncoder.Encode(payload.Replace(original, otherUserId.ToString()));

        return string.Join('.', parts);
    }
}
