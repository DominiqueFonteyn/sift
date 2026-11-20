using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sift.Api.IntegrationTests.Requirements;

public sealed class RequirementEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private const string ProblemJson = "application/problem+json";

    [Theory]
    [InlineData("GET", "/requirements")]
    [InlineData("POST", "/requirements")]
    [InlineData("DELETE", "/requirements/5b0e6a0e-6a58-4d36-9e5f-3b6a0d0c0b1a")]
    public async Task Anonymous_requests_get_a_401_problem(string method, string path)
    {
        using var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "POST" ? JsonContent.Create(new { text = "Remote-first", kind = "must-have" }) : null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task An_anonymous_delete_does_not_remove_the_requirement()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var anonymous = factory.CreateClient();
        var id = (await CreateAsync(owner, "Remote-first", "must-have")).GetProperty("id").GetGuid();

        var response = await anonymous.DeleteAsync($"/requirements/{id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(new[] { "Remote-first" }, Texts(await owner.GetFromJsonAsync<JsonElement>("/requirements")));
    }

    [Theory]
    [InlineData("must-have")]
    [InlineData("nice-to-have")]
    public async Task Creates_a_requirement_and_lists_it_with_its_kind(string kind)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/requirements", new { text = "  Remote-first ", kind });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Remote-first", created.GetProperty("text").GetString());
        Assert.Equal(kind, created.GetProperty("kind").GetString());
        var list = await client.GetFromJsonAsync<JsonElement>("/requirements");
        var item = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal(created.GetProperty("id").GetGuid(), item.GetProperty("id").GetGuid());
        Assert.Equal(kind, item.GetProperty("kind").GetString());
        Assert.Equal(1, list.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_text_is_a_400_problem_and_stores_nothing(string text)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateAsync(client, "Remote-first", "must-have");

        var response = await client.PostAsJsonAsync("/requirements", new { text, kind = "must-have" });

        await AssertValidationProblemAsync(response, "text");
        Assert.Equal(new[] { "Remote-first" }, Texts(await client.GetFromJsonAsync<JsonElement>("/requirements")));
    }

    [Fact]
    public async Task Text_over_the_maximum_length_is_a_400_problem()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/requirements", new { text = new string('a', 201), kind = "must-have" });

        await AssertValidationProblemAsync(response, "text");
        Assert.Empty(Texts(await client.GetFromJsonAsync<JsonElement>("/requirements")));
    }

    [Fact]
    public async Task Omitting_the_kind_is_a_400_problem_and_stores_nothing()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/requirements", new { text = "Kotlin" });

        await AssertValidationProblemAsync(response, "kind");
        Assert.Empty(Texts(await client.GetFromJsonAsync<JsonElement>("/requirements")));
    }

    [Fact]
    public async Task An_unknown_kind_is_a_400_problem_and_stores_nothing()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/requirements", new { text = "Kotlin", kind = "mandatory" });

        await AssertValidationProblemAsync(response, "kind");
        Assert.Empty(Texts(await client.GetFromJsonAsync<JsonElement>("/requirements")));
    }

    [Theory]
    [InlineData("remote-first")]
    [InlineData("REMOTE-FIRST")]
    [InlineData("  Remote-first  ")]
    public async Task A_duplicate_text_is_a_409_problem_and_stores_nothing(string text)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateAsync(client, "Remote-first", "must-have");

        var response = await client.PostAsJsonAsync("/requirements", new { text, kind = "must-have" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new[] { "Remote-first" }, Texts(await client.GetFromJsonAsync<JsonElement>("/requirements")));
    }

    [Fact]
    public async Task Two_users_may_hold_the_same_text()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateAsync(userA, "Remote-first", "must-have");

        var response = await userB.PostAsJsonAsync("/requirements", new { text = "Remote-first", kind = "must-have" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Listing_returns_only_the_callers_requirements()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateAsync(userA, "Remote-first", "must-have");
        await CreateAsync(userB, "Kotlin", "nice-to-have");

        var list = await userB.GetFromJsonAsync<JsonElement>("/requirements");

        Assert.Equal(new[] { "Kotlin" }, Texts(list));
        Assert.Equal(1, list.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Listing_is_paged_oldest_first()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        foreach (var text in new[] { "one", "two", "three" })
        {
            await CreateAsync(client, text, "nice-to-have");
        }

        var first = await client.GetFromJsonAsync<JsonElement>("/requirements?page=1&pageSize=2");
        var second = await client.GetFromJsonAsync<JsonElement>("/requirements?page=2&pageSize=2");

        Assert.Equal(new[] { "one", "two" }, Texts(first));
        Assert.Equal(new[] { "three" }, Texts(second));
        Assert.Equal(3, second.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task An_invalid_page_size_is_a_400_problem()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.GetAsync("/requirements?pageSize=101");

        await AssertValidationProblemAsync(response, "pageSize");
    }

    [Fact]
    public async Task Deletes_a_requirement_and_it_is_no_longer_listed()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateAsync(client, "Remote-first", "must-have");
        var kotlin = await CreateAsync(client, "Kotlin", "nice-to-have");

        var response = await client.DeleteAsync($"/requirements/{kotlin.GetProperty("id").GetGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(new[] { "Remote-first" }, Texts(await client.GetFromJsonAsync<JsonElement>("/requirements")));
    }

    [Fact]
    public async Task A_deleted_text_can_be_added_again()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var id = (await CreateAsync(client, "Kotlin", "nice-to-have")).GetProperty("id").GetGuid();
        await client.DeleteAsync($"/requirements/{id}");

        var response = await client.PostAsJsonAsync("/requirements", new { text = "Kotlin", kind = "nice-to-have" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_missing_requirement_is_a_404_problem()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.DeleteAsync($"/requirements/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_malformed_id_is_indistinguishable_from_a_missing_one()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var malformed = await client.DeleteAsync("/requirements/not-a-guid");
        var missing = await client.DeleteAsync($"/requirements/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
        Assert.Equal(ProblemJson, malformed.Content.Headers.ContentType?.MediaType);
        Assert.Equal(await Normalised(missing), await Normalised(malformed));
    }

    [Fact]
    public async Task Another_users_requirement_cannot_be_deleted_and_stays_in_the_owners_list()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var other = await AuthenticatedClient.CreateSignedInAsync(factory);
        var id = (await CreateAsync(owner, "Remote-first", "must-have")).GetProperty("id").GetGuid();

        var foreign = await other.DeleteAsync($"/requirements/{id}");
        var missing = await other.DeleteAsync($"/requirements/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(ProblemJson, foreign.Content.Headers.ContentType?.MediaType);
        Assert.Equal(await Normalised(missing), await Normalised(foreign));
        Assert.Equal(new[] { "Remote-first" }, Texts(await owner.GetFromJsonAsync<JsonElement>("/requirements")));
    }

    [Fact]
    public async Task A_supplied_owner_id_is_ignored_in_favour_of_the_authenticated_identity()
    {
        using var attacker = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var victim = await AuthenticatedClient.CreateSignedInAsync(factory);
        var victimId = await AuthenticatedClient.GetUserIdAsync(victim);

        var response = await attacker.PostAsJsonAsync("/requirements", new { text = "Spoofed", kind = "must-have", ownerId = victimId, userId = victimId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(new[] { "Spoofed" }, Texts(await attacker.GetFromJsonAsync<JsonElement>("/requirements")));
        Assert.Empty(Texts(await victim.GetFromJsonAsync<JsonElement>("/requirements")));
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(field, Assert.Single(errors.EnumerateObject()).Name);
    }

    private static string?[] Texts(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("text").GetString()).ToArray();

    private static async Task<JsonElement> CreateAsync(HttpClient client, string text, string kind)
    {
        var response = await client.PostAsJsonAsync("/requirements", new { text, kind });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // traceId and instance (the requested path) differ per request; everything else must match.
    private static async Task<string> Normalised(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        body!.Remove("traceId");
        body.Remove("instance");
        return string.Join("|", body.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
    }
}
