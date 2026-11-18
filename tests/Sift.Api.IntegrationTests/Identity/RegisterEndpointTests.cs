using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Sift.Application.Identity;
using Sift.Domain.Identity;

namespace Sift.Api.IntegrationTests.Identity;

public sealed class RegisterEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private const string ValidPassword = "correct horse battery";

    [Fact]
    public async Task Registers_an_account_and_the_credentials_can_be_verified()
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();

        var response = await Register(client, email, ValidPassword);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());

        using var scope = factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<IUserRepository>()
            .FindByEmailAsync(Email.Create(email), CancellationToken.None);
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        Assert.NotNull(stored);
        Assert.True(hasher.Verify(ValidPassword, stored.PasswordHash));
        Assert.False(hasher.Verify("not the password", stored.PasswordHash));
    }

    [Fact]
    public async Task Never_returns_the_password_or_its_hash()
    {
        using var client = factory.CreateClient();

        var response = await Register(client, UniqueEmail(), ValidPassword);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(ValidPassword, raw);
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Persists_only_a_salted_hash_never_the_clear_text_password()
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();

        await Register(client, email, ValidPassword);
        var first = ReadStoredHash(email);

        var otherEmail = UniqueEmail();
        await Register(client, otherEmail, ValidPassword);
        var second = ReadStoredHash(otherEmail);

        Assert.DoesNotContain(ValidPassword, first);
        Assert.NotEqual(ValidPassword, first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task Rejects_a_duplicate_email_with_a_409_problem_and_creates_no_second_account()
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();

        await Register(client, email, ValidPassword);
        var response = await Register(client, email.ToUpperInvariant(), "another valid password");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.Equal(1, CountUsers(email));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    [InlineData("")]
    public async Task Rejects_a_weak_password_with_a_400_problem_naming_the_password_field(string password)
    {
        using var client = factory.CreateClient();
        var email = UniqueEmail();

        var response = await Register(client, email, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.True(problem.GetProperty("errors").TryGetProperty("password", out _));
        Assert.Equal(0, CountUsers(email));
    }

    [Fact]
    public async Task Rejects_an_invalid_email_with_a_400_problem_naming_the_email_field()
    {
        using var client = factory.CreateClient();

        var response = await Register(client, "not-an-email", ValidPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("email", out _));
    }

    [Fact]
    public async Task Is_reachable_without_authentication()
    {
        using var client = factory.CreateClient();

        var response = await Register(client, UniqueEmail(), ValidPassword);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static Task<HttpResponseMessage> Register(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/auth/register", new { email, password });

    private string ReadStoredHash(string email)
    {
        using var connection = new SqliteConnection(factory.DatabaseConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "select PasswordHash from Users where Email = $email";
        command.Parameters.AddWithValue("$email", email.ToLowerInvariant());

        return (string)command.ExecuteScalar()!;
    }

    private long CountUsers(string email)
    {
        using var connection = new SqliteConnection(factory.DatabaseConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from Users where Email = $email";
        command.Parameters.AddWithValue("$email", email.ToLowerInvariant());

        return (long)command.ExecuteScalar()!;
    }
}
