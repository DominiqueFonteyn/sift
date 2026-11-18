using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Sift.Application.Common;
using Sift.Infrastructure.Identity;

namespace Sift.Api.IntegrationTests.Identity;

public sealed class LoginEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private const string Password = "correct horse battery";

    [Fact]
    public async Task Logs_in_and_returns_a_jwt_with_the_user_id_and_an_expiry()
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();
        var userId = await RegisterAsync(client, email);

        var response = await LoginAsync(client, email, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("accessToken").GetString());

        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.Equal(userId.ToString(), token.Subject);
        Assert.True(token.ValidTo > DateTime.UtcNow);
        Assert.Equal(body.GetProperty("expiresAt").GetDateTimeOffset().UtcDateTime, token.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Email_matching_is_case_insensitive()
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        var response = await LoginAsync(client, email.ToUpperInvariant(), Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_give_the_same_401_problem()
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        var wrongPassword = await LoginAsync(client, email, "not the password");
        var unknownEmail = await LoginAsync(client, UniqueEmail(), Password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal("application/problem+json", wrongPassword.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/problem+json", unknownEmail.Content.Headers.ContentType?.MediaType);
        Assert.Equal(await ReadProblemAsync(wrongPassword), await ReadProblemAsync(unknownEmail));
    }

    [Fact]
    public async Task The_401_problem_does_not_mention_the_email_or_whether_it_exists()
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();

        var response = await LoginAsync(client, email, Password);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(email, raw);
        Assert.DoesNotContain("exist", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unknown", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not found", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("not-an-email", "whatever")]
    public async Task Missing_or_malformed_credentials_are_also_a_401(string? email, string? password)
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_valid_token_reaches_a_protected_endpoint_and_only_returns_the_callers_own_account()
    {
        using var client = factory.CreateClient();
        var firstEmail = UniqueEmail();
        var secondEmail = UniqueEmail();
        var firstId = await RegisterAsync(client, firstEmail);
        var secondId = await RegisterAsync(client, secondEmail);

        var firstMe = await GetMeAsync(client, await LoginTokenAsync(client, firstEmail));
        var secondMe = await GetMeAsync(client, await LoginTokenAsync(client, secondEmail));

        Assert.Equal(HttpStatusCode.OK, firstMe.StatusCode);
        var first = await firstMe.Content.ReadFromJsonAsync<JsonElement>();
        var second = await secondMe.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(firstId, first.GetProperty("id").GetGuid());
        Assert.Equal(firstEmail, first.GetProperty("email").GetString());
        Assert.Equal(secondId, second.GetProperty("id").GetGuid());
        Assert.Equal(secondEmail, second.GetProperty("email").GetString());
    }

    [Fact]
    public async Task A_protected_endpoint_without_a_token_is_a_401_problem()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task An_expired_token_is_a_401_problem()
    {
        using var client = factory.CreateClient();
        var userId = await RegisterAsync(client, UniqueEmail());
        var longAgo = new FixedClock(DateTimeOffset.UtcNow.AddHours(-3));
        var token = new JwtTokenIssuer(TestOptions(), longAgo).Issue(userId);

        var response = await GetMeAsync(client, token.Value);

        Assert.True(token.ExpiresAt < DateTimeOffset.UtcNow);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_a_401()
    {
        using var client = factory.CreateClient();
        var userId = await RegisterAsync(client, UniqueEmail());
        var forged = new JwtTokenIssuer(
            TestOptions() with { SigningKey = "a-completely-different-signing-key-0123456789" },
            new FixedClock(DateTimeOffset.UtcNow)).Issue(userId);

        var response = await GetMeAsync(client, forged.Value);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_and_login_stay_reachable_without_a_token()
    {
        using var client = factory.CreateClient();

        var login = await LoginAsync(client, UniqueEmail(), Password);
        var register = await client.PostAsJsonAsync("/auth/register", new { email = UniqueEmail(), password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [Fact]
    public async Task Only_health_register_and_login_are_anonymous_and_everything_else_is_protected()
    {
        using var client = factory.CreateClient();

        var health = await client.GetAsync("/health");
        var register = await client.PostAsJsonAsync("/auth/register", new { email = UniqueEmail(), password = Password });
        var login = await LoginAsync(client, UniqueEmail(), Password);
        var me = await client.GetAsync("/auth/me");
        var unknown = await client.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
    }

    private static JwtOptions TestOptions() => new() { SigningKey = SiftApiFactory.TestSigningKey };

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/auth/login", new { email, password });

    private static async Task<Guid> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<string> LoginTokenAsync(HttpClient client, string email)
    {
        var response = await LoginAsync(client, email, Password);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
    }

    private static Task<HttpResponseMessage> GetMeAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client.SendAsync(request);
    }

    private static async Task<(int? Status, string? Title, string? Detail, string? Type)> ReadProblemAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        return (
            problem.GetProperty("status").GetInt32(),
            problem.GetProperty("title").GetString(),
            problem.GetProperty("detail").GetString(),
            problem.GetProperty("type").GetString());
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
