using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Sift.Api.IntegrationTests.Vacancies;

public sealed class VacancyDeleteEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    [Fact]
    public async Task The_owner_gets_204_with_no_body_and_the_vacancy_leaves_the_list_and_total()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var keep = await CreateAsync(client, "Keep Me");
        var doomed = await CreateAsync(client, "Backend Engineer");
        Assert.Equal(2, (await ListAsync(client)).Total);

        var response = await client.DeleteAsync($"/vacancies/{doomed}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        var list = await ListAsync(client);
        Assert.Equal(1, list.Total);
        Assert.Equal([keep], list.Ids);
    }

    [Fact]
    public async Task A_deleted_vacancy_can_no_longer_be_opened()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var id = await CreateAsync(client, "Backend Engineer");

        await client.DeleteAsync($"/vacancies/{id}");
        var response = await client.GetAsync($"/vacancies/{id}");

        await AssertNotFoundProblem(response);
    }

    [Fact]
    public async Task Deleting_twice_is_a_404_problem_and_other_vacancies_are_unaffected()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var keep = await CreateAsync(client, "Keep Me");
        var doomed = await CreateAsync(client, "Backend Engineer");
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/vacancies/{doomed}")).StatusCode);

        var second = await client.DeleteAsync($"/vacancies/{doomed}");

        await AssertNotFoundProblem(second);
        var list = await ListAsync(client);
        Assert.Equal(1, list.Total);
        Assert.Equal([keep], list.Ids);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Another_users_vacancy_cannot_be_deleted_and_survives_untouched(int times)
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var other = await AuthenticatedClient.CreateSignedInAsync(factory);
        var id = await CreateAsync(owner, "Secret Title Zebra", "Secret Company Yak", "Secret text Walrus");

        for (var attempt = 0; attempt < times; attempt++)
        {
            var response = await other.DeleteAsync($"/vacancies/{id}");

            await AssertNotFoundProblem(response);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Zebra", body);
        }

        var list = await ListAsync(owner);
        Assert.Equal(1, list.Total);
        Assert.Equal([id], list.Ids);
        var detail = await owner.GetFromJsonAsync<JsonElement>($"/vacancies/{id}");
        Assert.Equal("Secret Title Zebra", detail.GetProperty("title").GetString());
        Assert.Equal("Secret Company Yak", detail.GetProperty("company").GetString());
        Assert.Equal("Secret text Walrus", detail.GetProperty("rawText").GetString());
    }

    [Fact]
    public async Task An_unknown_id_is_indistinguishable_from_someone_elses_vacancy()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var other = await AuthenticatedClient.CreateSignedInAsync(factory);
        var id = await CreateAsync(owner, "Backend Engineer");

        var foreign = await other.DeleteAsync($"/vacancies/{id}");
        var unknown = await other.DeleteAsync($"/vacancies/{Guid.NewGuid()}");

        await AssertNotFoundProblem(unknown);
        Assert.Equal(await Normalised(unknown), await Normalised(foreign));
    }

    [Theory]
    [InlineData("not-a-uuid")]
    [InlineData("123")]
    public async Task A_malformed_id_is_a_404_problem_not_a_400(string id)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var malformed = await client.DeleteAsync($"/vacancies/{id}");
        var unknown = await client.DeleteAsync($"/vacancies/{Guid.NewGuid()}");

        await AssertNotFoundProblem(malformed);
        Assert.Equal(await Normalised(unknown), await Normalised(malformed));
    }

    [Fact]
    public async Task Without_a_token_the_request_is_a_401_problem_and_the_vacancy_survives()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        var id = await CreateAsync(owner, "Backend Engineer");
        using var anonymous = factory.CreateClient();

        var response = await anonymous.DeleteAsync($"/vacancies/{id}");

        AssertUnauthorized(response);
        Assert.Equal([id], (await ListAsync(owner)).Ids);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("expired")]
    public async Task A_malformed_or_expired_token_is_a_401_problem_and_the_vacancy_survives(string kind)
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        var userId = await AuthenticatedClient.GetUserIdAsync(owner);
        var id = await CreateAsync(owner, "Backend Engineer");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", kind == "malformed" ? "not-a-jwt" : ExpiredToken(userId));

        var response = await client.DeleteAsync($"/vacancies/{id}");

        AssertUnauthorized(response);
        Assert.Equal([id], (await ListAsync(owner)).Ids);
    }

    private static void AssertUnauthorized(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task AssertNotFoundProblem(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(404, body.GetProperty("status").GetInt32());
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string title, string company = "Acme", string rawText = "text")
    {
        var response = await client.PostAsJsonAsync("/vacancies", new { title, company, rawText });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<(int Total, Guid[] Ids)> ListAsync(HttpClient client)
    {
        var body = await client.GetFromJsonAsync<JsonElement>("/vacancies?pageSize=100");
        return (
            body.GetProperty("total").GetInt32(),
            body.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Order().ToArray());
    }

    private static async Task<string> Normalised(HttpResponseMessage response)
    {
        var body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await response.Content.ReadAsStringAsync())!;
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
