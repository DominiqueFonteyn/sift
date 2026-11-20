using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Sift.Api.IntegrationTests.Vacancies;

public sealed class VacancyDetailEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private static readonly DateTimeOffset NearMidnight = new(2026, 3, 10, 0, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task The_owner_gets_the_vacancy_in_full()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(NearMidnight);

        Guid id;
        try
        {
            id = await CreateAsync(client, "Backend Engineer", "Acme", "We need C# and SQL");
        }
        finally
        {
            factory.Clock.Reset();
        }

        var response = await client.GetAsync($"/vacancies/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(["company", "createdAt", "id", "rawText", "title"], body.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(id, body.GetProperty("id").GetGuid());
        Assert.Equal("Backend Engineer", body.GetProperty("title").GetString());
        Assert.Equal("Acme", body.GetProperty("company").GetString());
        Assert.Equal("We need C# and SQL", body.GetProperty("rawText").GetString());
        Assert.Equal(NearMidnight, body.GetProperty("createdAt").GetDateTimeOffset());
        Assert.Equal(TimeSpan.Zero, body.GetProperty("createdAt").GetDateTimeOffset().Offset);
    }

    [Fact]
    public async Task Title_and_company_are_trimmed_and_the_text_is_returned_exactly_as_stored()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        const string text = "  Line one\n\n  Line two  \n";
        var id = await CreateAsync(client, "  Backend Engineer  ", " Acme ", text);

        var body = await client.GetFromJsonAsync<JsonElement>($"/vacancies/{id}");

        Assert.Equal("Backend Engineer", body.GetProperty("title").GetString());
        Assert.Equal("Acme", body.GetProperty("company").GetString());
        Assert.Equal(text, body.GetProperty("rawText").GetString());
    }

    [Fact]
    public async Task A_long_text_with_mixed_line_endings_and_non_ascii_round_trips_untruncated()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var text = "\t start \r\n" + string.Concat(Enumerable.Repeat("Zeile mit Umlauten äöü 日本語 😀\n", 600)) + "  end  \r\n";
        var id = await CreateAsync(client, "Long", "Acme", text);

        var body = await client.GetFromJsonAsync<JsonElement>($"/vacancies/{id}");

        Assert.True(text.Length > 15000);
        Assert.Equal(text, body.GetProperty("rawText").GetString());
    }

    [Fact]
    public async Task Another_users_vacancy_is_a_404_that_leaks_nothing_and_matches_the_unknown_case()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var other = await AuthenticatedClient.CreateSignedInAsync(factory);
        var id = await CreateAsync(owner, "Secret Title Zebra", "Secret Company Yak", "Secret text Walrus");

        var foreign = await other.GetAsync($"/vacancies/{id}");
        var unknown = await other.GetAsync($"/vacancies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("application/problem+json", foreign.Content.Headers.ContentType?.MediaType);
        var foreignBody = await foreign.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Zebra", foreignBody);
        Assert.DoesNotContain("Yak", foreignBody);
        Assert.DoesNotContain("Walrus", foreignBody);
        Assert.Equal(await Normalised(unknown), Normalise(foreignBody));
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/vacancies/{id}")).StatusCode);
    }

    [Theory]
    [InlineData("not-a-uuid")]
    [InlineData("123")]
    [InlineData("%20")]
    public async Task A_malformed_id_is_indistinguishable_from_an_unknown_one(string id)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var malformed = await client.GetAsync($"/vacancies/{id}");
        var unknown = await client.GetAsync($"/vacancies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
        Assert.Equal("application/problem+json", malformed.Content.Headers.ContentType?.MediaType);
        Assert.Equal(await Normalised(unknown), await Normalised(malformed));
    }

    [Fact]
    public async Task Without_a_token_the_request_is_a_401_problem_with_no_vacancy_data()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        var id = await CreateAsync(owner, "Anonymous Title Giraffe", "Acme", "text");
        using var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync($"/vacancies/{id}");

        await AssertUnauthorized(response, "Giraffe");
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("expired")]
    public async Task A_malformed_or_expired_token_is_a_401_problem_with_no_vacancy_data(string kind)
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        var userId = await AuthenticatedClient.GetUserIdAsync(owner);
        var id = await CreateAsync(owner, "Expired Title Giraffe", "Acme", "text");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", kind == "malformed" ? "not-a-jwt" : ExpiredToken(userId));

        var response = await client.GetAsync($"/vacancies/{id}");

        await AssertUnauthorized(response, "Giraffe");
    }

    private static async Task AssertUnauthorized(HttpResponseMessage response, string leakedWord)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(leakedWord, body);
        Assert.Equal(401, JsonDocument.Parse(body).RootElement.GetProperty("status").GetInt32());
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string title, string company, string rawText)
    {
        var response = await client.PostAsJsonAsync("/vacancies", new { title, company, rawText });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<string> Normalised(HttpResponseMessage response) => Normalise(await response.Content.ReadAsStringAsync());

    // traceId and instance (the requested path) differ per request; everything else must match.
    private static string Normalise(string json)
    {
        var body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        body.Remove("traceId");
        body.Remove("instance");
        return string.Join("|", body.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static string ExpiredToken(Guid userId)
    {
        var now = DateTime.UtcNow;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "sift",
            Audience = "sift",
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = userId.ToString() },
            IssuedAt = now.AddHours(-2),
            NotBefore = now.AddHours(-2),
            Expires = now.AddMinutes(-1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SiftApiFactory.TestSigningKey)),
                SecurityAlgorithms.HmacSha256)
        });
    }
}
