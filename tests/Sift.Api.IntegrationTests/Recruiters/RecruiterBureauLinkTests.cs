using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Sift.Api.IntegrationTests.Recruiters;

public sealed class RecruiterBureauLinkTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    [Fact]
    public async Task Links_a_recruiter_to_my_bureau_and_lists_it_with_the_bureau()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var bureauId = await CreateBureauAsync(client, "Acme Recruitment");

        var response = await client.PostAsJsonAsync("/recruiters", new { name = "Jane Doe", bureauId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(bureauId, created.GetProperty("bureau").GetProperty("id").GetGuid());
        Assert.Equal("Acme Recruitment", created.GetProperty("bureau").GetProperty("name").GetString());

        var item = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/recruiters")).GetProperty("items").EnumerateArray());
        Assert.Equal(created.GetProperty("id").GetGuid(), item.GetProperty("id").GetGuid());
        Assert.Equal(bureauId, item.GetProperty("bureau").GetProperty("id").GetGuid());
        Assert.Equal("Acme Recruitment", item.GetProperty("bureau").GetProperty("name").GetString());
    }

    [Fact]
    public async Task A_recruiter_without_a_bureau_has_no_bureau_property_even_when_I_own_bureaus()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateBureauAsync(client, "Acme Recruitment");

        var response = await client.PostAsJsonAsync("/recruiters", new { name = "Sam Lee" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("bureau", out _));
        var item = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/recruiters")).GetProperty("items").EnumerateArray());
        Assert.False(item.TryGetProperty("bureau", out _));
    }

    [Theory]
    [InlineData("{\"name\":\"Sam Lee\",\"bureauId\":\"\"}")]
    [InlineData("{\"name\":\"Sam Lee\",\"bureauId\":\"   \"}")]
    [InlineData("{\"name\":\"Sam Lee\",\"bureauId\":null}")]
    public async Task An_empty_whitespace_or_null_bureau_id_means_no_bureau(string json)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateBureauAsync(client, "Acme Recruitment");

        var response = await client.PostAsync("/recruiters", new StringContent(json, new MediaTypeHeaderValue("application/json")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("bureau", out _));
        var item = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/recruiters")).GetProperty("items").EnumerateArray());
        Assert.False(item.TryGetProperty("bureau", out _));
    }

    [Fact]
    public async Task Cannot_link_a_recruiter_to_another_users_bureau()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        var foreignBureauId = await CreateBureauAsync(userB, "Beta Talent");
        var before = await TotalAsync(userA);

        var response = await userA.PostAsJsonAsync("/recruiters", new { name = "Jane Doe", bureauId = foreignBureauId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Beta Talent", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await TotalAsync(userA));
        Assert.Equal(0, await TotalAsync(userB));
    }

    [Fact]
    public async Task A_missing_bureau_is_indistinguishable_from_another_users_bureau()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        var foreignBureauId = await CreateBureauAsync(userB, "Beta Talent");

        var foreign = await userA.PostAsJsonAsync("/recruiters", new { name = "Jane Doe", bureauId = foreignBureauId });
        var missing = await userA.PostAsJsonAsync("/recruiters", new { name = "Jane Doe", bureauId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
        Assert.Equal(await Normalised(missing), await Normalised(foreign));
        Assert.Equal(0, await TotalAsync(userA));
    }

    [Fact]
    public async Task A_malformed_bureau_id_is_a_400_with_an_error_under_bureau_id()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/recruiters", new { name = "Jane Doe", bureauId = "not-a-uuid" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal("bureauId", Assert.Single(errors.EnumerateObject()).Name);
        Assert.Equal(0, await TotalAsync(client));
    }

    [Fact]
    public async Task Validation_runs_before_the_ownership_lookup()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        var foreignBureauId = await CreateBureauAsync(userB, "Beta Talent");

        var response = await userA.PostAsJsonAsync("/recruiters", new { name = " ", bureauId = foreignBureauId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Beta Talent", body);
        var errors = JsonDocument.Parse(body).RootElement.GetProperty("errors");
        Assert.Equal("name", Assert.Single(errors.EnumerateObject()).Name);
        Assert.Equal(0, await TotalAsync(userA));
    }

    [Fact]
    public async Task The_bureau_name_in_the_list_never_comes_from_another_user()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        var bureauA = await CreateBureauAsync(userA, "Alpha");
        await CreateBureauAsync(userB, "Beta Talent");
        await userA.PostAsJsonAsync("/recruiters", new { name = "Jane Doe", bureauId = bureauA });

        var listForB = await userB.GetStringAsync("/recruiters");

        Assert.DoesNotContain("Alpha", listForB);
        Assert.DoesNotContain("Jane Doe", listForB);
    }

    [Fact]
    public async Task Anonymous_posts_with_a_bureau_get_a_401_problem()
    {
        using var anonymous = factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/recruiters", new { name = "Nobody", bureauId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<Guid> CreateBureauAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/bureaus", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<int> TotalAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/recruiters")).GetProperty("total").GetInt32();

    // traceId and instance differ per request; everything else must match.
    private static async Task<string> Normalised(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        body!.Remove("traceId");
        body.Remove("instance");
        return string.Join("|", body.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
    }
}
