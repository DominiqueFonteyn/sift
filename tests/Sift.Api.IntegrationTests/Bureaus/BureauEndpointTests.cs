using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sift.Api.IntegrationTests.Bureaus;

public sealed class BureauEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    [Fact]
    public async Task A_bureau_without_a_website_has_no_website_property_anywhere()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/bureaus", new { name = "Acme Recruitment" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        await AssertNoWebsiteEverywhereAsync(client, created);
    }

    [Fact]
    public async Task A_null_website_means_no_website()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/bureaus", new { name = "Acme Recruitment", website = (string?)null });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await AssertNoWebsiteEverywhereAsync(client, await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    [InlineData("\t \t")]
    public async Task A_blank_website_is_treated_as_not_given(string website)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/bureaus", new { name = "Acme Recruitment", website });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await AssertNoWebsiteEverywhereAsync(client, await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    [Fact]
    public async Task A_website_round_trips_through_create_list_and_get()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/bureaus", new { name = "Acme Recruitment", website = "https://acme.example" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal("Acme Recruitment", created.GetProperty("name").GetString());
        Assert.Equal("https://acme.example", created.GetProperty("website").GetString());

        var list = await client.GetFromJsonAsync<JsonElement>("/bureaus");
        var listed = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal("https://acme.example", listed.GetProperty("website").GetString());

        var fetched = await client.GetFromJsonAsync<JsonElement>($"/bureaus/{id}");
        Assert.Equal("https://acme.example", fetched.GetProperty("website").GetString());
    }

    // A website with surrounding spaces is trimmed, consistent with the name and with recruiters' linkedInUrl.
    [Fact]
    public async Task A_website_with_surrounding_spaces_is_trimmed_when_stored()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/bureaus", new { name = "  Acme Recruitment ", website = "  https://acme.example  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Acme Recruitment", created.GetProperty("name").GetString());
        Assert.Equal("https://acme.example", created.GetProperty("website").GetString());
        var fetched = await client.GetFromJsonAsync<JsonElement>($"/bureaus/{created.GetProperty("id").GetGuid()}");
        Assert.Equal("https://acme.example", fetched.GetProperty("website").GetString());
    }

    [Fact]
    public async Task Duplicate_bureau_names_are_allowed()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        Assert.Equal(0, await TotalAsync(client));

        var first = await client.PostAsJsonAsync("/bureaus", new { name = "Acme Recruitment" });
        var second = await client.PostAsJsonAsync("/bureaus", new { name = "Acme Recruitment" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.NotEqual(firstId, secondId);
        Assert.Equal(2, await TotalAsync(client));
    }

    [Theory]
    [InlineData("no-name", "name")]
    [InlineData("blank-name", "name")]
    [InlineData("name-201", "name")]
    [InlineData("website-relative", "website")]
    [InlineData("website-ftp", "website")]
    [InlineData("website-2049", "website")]
    public async Task Invalid_bureau_input_is_a_400_problem_and_adds_nothing(string input, string field)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        Assert.Equal(0, await TotalAsync(client));

        var response = await client.PostAsJsonAsync("/bureaus", Payload(input));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out var messages), $"No error under {field}.");
        Assert.NotEmpty(messages.EnumerateArray());
        Assert.Equal(0, await TotalAsync(client));
    }

    [Fact]
    public async Task A_website_of_2048_characters_is_accepted()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/bureaus", new { name = "Acme", website = LongWebsite(2048) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Name_and_website_errors_are_reported_together()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/bureaus", new { name = " ", website = "acme.example" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("name", out _));
        Assert.True(errors.TryGetProperty("website", out _));
        Assert.Equal(0, await TotalAsync(client));
    }

    [Fact]
    public async Task Users_only_see_their_own_bureaus_and_websites()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        var alpha = await userA.PostAsJsonAsync("/bureaus", new { name = "Alpha Search", website = "https://alpha.example" });
        await userB.PostAsJsonAsync("/bureaus", new { name = "Beta Talent" });
        var alphaId = (await alpha.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var list = await userB.GetFromJsonAsync<JsonElement>("/bureaus");
        var foreign = await userB.GetAsync($"/bureaus/{alphaId}");

        var only = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal("Beta Talent", only.GetProperty("name").GetString());
        Assert.Equal(1, list.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal("application/problem+json", foreign.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("alpha", await foreign.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    // FR-1.4: a bureau that exists for someone else must be indistinguishable from one that never existed.
    [Fact]
    public async Task An_unknown_id_and_another_users_id_return_the_same_404_problem()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var other = await AuthenticatedClient.CreateSignedInAsync(factory);
        var created = await owner.PostAsJsonAsync("/bureaus", new { name = "Acme", website = "https://acme.example" });
        var ownersId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var foreign = await other.GetAsync($"/bureaus/{ownersId}");
        var unknown = await other.GetAsync($"/bureaus/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(unknown.Content.Headers.ContentType?.MediaType, foreign.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/problem+json", unknown.Content.Headers.ContentType?.MediaType);
        Assert.Equal(await Normalised(unknown), await Normalised(foreign));
    }

    public static TheoryData<string, string, string> UnauthorizedCases()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var token in new[] { "missing", "malformed", "expired" })
        {
            data.Add(token, "POST", "/bureaus");
            data.Add(token, "GET", "/bureaus");
            data.Add(token, "GET", "/bureaus/5b0e6a0e-6a58-4d36-9e5f-3b6a0d0c0b1a");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(UnauthorizedCases))]
    public async Task Missing_malformed_or_expired_tokens_get_a_401_problem(string tokenState, string method, string path)
    {
        using var client = await ClientWithTokenAsync(tokenState);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "POST" ? JsonContent.Create(new { name = "Acme" }) : null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // The issuer reads the injectable clock, so a login while it is pinned in the past yields a token that is already expired.
    private async Task<HttpClient> ClientWithTokenAsync(string tokenState)
    {
        if (tokenState == "expired")
        {
            factory.Clock.Set(DateTimeOffset.UtcNow.AddHours(-3));
            try
            {
                return await AuthenticatedClient.CreateSignedInAsync(factory);
            }
            finally
            {
                factory.Clock.Reset();
            }
        }

        var client = factory.CreateClient();
        if (tokenState == "malformed")
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");
        }

        return client;
    }

    private static object Payload(string input) => input switch
    {
        "no-name" => new { website = "https://acme.example" },
        "blank-name" => new { name = " \t  " },
        "name-201" => new { name = new string('a', 201) },
        "website-relative" => new { name = "Acme", website = "acme.example" },
        "website-ftp" => new { name = "Acme", website = "ftp://acme.example" },
        "website-2049" => new { name = "Acme", website = LongWebsite(2049) },
        _ => throw new ArgumentOutOfRangeException(nameof(input), input, null)
    };

    private static string LongWebsite(int length)
    {
        const string prefix = "https://acme.example/";
        return prefix + new string('a', length - prefix.Length);
    }

    private static async Task<int> TotalAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/bureaus")).GetProperty("total").GetInt32();

    private static async Task AssertNoWebsiteEverywhereAsync(HttpClient client, JsonElement created)
    {
        Assert.False(created.TryGetProperty("website", out _), "POST response has a website property.");
        var id = created.GetProperty("id").GetGuid();

        var list = await client.GetFromJsonAsync<JsonElement>("/bureaus");
        Assert.All(list.GetProperty("items").EnumerateArray(), item => Assert.False(item.TryGetProperty("website", out _)));

        var fetched = await client.GetFromJsonAsync<JsonElement>($"/bureaus/{id}");
        Assert.False(fetched.TryGetProperty("website", out _), "GET response has a website property.");
    }

    private static async Task<string> Normalised(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        body!.Remove("traceId");
        body.Remove("instance");
        return string.Join("|", body.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
    }
}
