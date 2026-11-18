using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sift.Api.IntegrationTests.Bureaus;

public sealed class BureauOwnershipTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    [Theory]
    [InlineData("GET", "/bureaus")]
    [InlineData("POST", "/bureaus")]
    [InlineData("GET", "/bureaus/5b0e6a0e-6a58-4d36-9e5f-3b6a0d0c0b1a")]
    [InlineData("GET", "/auth/me")]
    public async Task Anonymous_requests_get_a_401_problem(string method, string path)
    {
        using var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "POST" ? JsonContent.Create(new { name = "Acme" }) : null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Another_users_bureau_is_indistinguishable_from_a_missing_one()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var other = await AuthenticatedClient.CreateSignedInAsync(factory);
        var id = (await CreateAsync(owner, "Acme")).GetProperty("id").GetGuid();

        var foreign = await other.GetAsync($"/bureaus/{id}");
        var missing = await other.GetAsync($"/bureaus/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", foreign.Content.Headers.ContentType?.MediaType);
        Assert.Equal(await Normalised(missing), await Normalised(foreign));
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/bureaus/{id}")).StatusCode);
    }

    [Fact]
    public async Task A_malformed_id_is_indistinguishable_from_a_missing_one()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var malformed = await client.GetAsync("/bureaus/not-a-guid");
        var missing = await client.GetAsync($"/bureaus/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
        Assert.Equal("application/problem+json", malformed.Content.Headers.ContentType?.MediaType);
        Assert.Equal(await Normalised(missing), await Normalised(malformed));
    }

    [Fact]
    public async Task Listing_returns_only_the_callers_bureaus()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateAsync(userA, "A-one");
        await CreateAsync(userA, "A-two");
        await CreateAsync(userB, "B-one");

        var list = await userB.GetFromJsonAsync<JsonElement>("/bureaus");

        Assert.Equal(new[] { "B-one" }, Names(list));
        Assert.Equal(1, list.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Listing_is_paged_oldest_first_and_counts_only_the_callers_bureaus()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var other = await AuthenticatedClient.CreateSignedInAsync(factory);
        foreach (var name in new[] { "one", "two", "three", "four", "five" })
        {
            await CreateAsync(owner, name);
        }
        await CreateAsync(other, "foreign");

        var first = await owner.GetFromJsonAsync<JsonElement>("/bureaus?page=1&pageSize=2");
        var last = await owner.GetFromJsonAsync<JsonElement>("/bureaus?page=3&pageSize=2");
        var beyond = await owner.GetFromJsonAsync<JsonElement>("/bureaus?page=4&pageSize=2");
        var defaults = await owner.GetFromJsonAsync<JsonElement>("/bureaus");

        Assert.Equal(new[] { "one", "two" }, Names(first));
        Assert.Equal(new[] { "five" }, Names(last));
        Assert.Empty(Names(beyond));
        Assert.All(new[] { first, last, beyond, defaults }, page => Assert.Equal(5, page.GetProperty("total").GetInt32()));
        Assert.Equal(3, last.GetProperty("page").GetInt32());
        Assert.Equal(2, last.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, defaults.GetProperty("page").GetInt32());
        Assert.Equal(20, defaults.GetProperty("pageSize").GetInt32());
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("page=abc", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    public async Task An_invalid_page_or_page_size_is_a_400_problem(string query, string field)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.GetAsync($"/bureaus?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(field, Assert.Single(errors.EnumerateObject()).Name);
    }

    [Fact]
    public async Task A_supplied_owner_id_is_ignored_in_favour_of_the_authenticated_identity()
    {
        using var attacker = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var victim = await AuthenticatedClient.CreateSignedInAsync(factory);
        var victimId = await AuthenticatedClient.GetUserIdAsync(victim);

        var response = await attacker.PostAsJsonAsync("/bureaus", new { name = "Spoofed", ownerId = victimId, userId = victimId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await attacker.GetAsync($"/bureaus/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await victim.GetAsync($"/bureaus/{id}")).StatusCode);
        Assert.Empty(Names(await victim.GetFromJsonAsync<JsonElement>("/bureaus")));
    }

    [Fact]
    public async Task A_blank_name_is_a_400_problem()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/bureaus", new { name = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        var name = Assert.Single(errors.EnumerateObject());
        Assert.Equal("name", name.Name);
        Assert.Equal("A bureau needs a name.", Assert.Single(name.Value.EnumerateArray()).GetString());
        Assert.Empty(Names(await client.GetFromJsonAsync<JsonElement>("/bureaus")));
    }

    private static string?[] Names(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToArray();

    private static async Task<JsonElement> CreateAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/bureaus", new { name });
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
