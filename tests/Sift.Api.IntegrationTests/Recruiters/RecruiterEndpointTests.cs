using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sift.Api.IntegrationTests.Recruiters;

public sealed class RecruiterEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    [Fact]
    public async Task Adds_a_recruiter_with_a_trimmed_name_and_lists_it()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/recruiters", new { name = "  Jane Doe  ", email = "jane@acme.example" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Jane Doe", created.GetProperty("name").GetString());
        Assert.Equal("jane@acme.example", created.GetProperty("email").GetString());
        Assert.NotEqual(Guid.Empty, created.GetProperty("id").GetGuid());
        Assert.True(created.TryGetProperty("createdAt", out _));

        var list = await client.GetFromJsonAsync<JsonElement>("/recruiters");
        var item = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal(created.GetProperty("id").GetGuid(), item.GetProperty("id").GetGuid());
        Assert.Equal("Jane Doe", item.GetProperty("name").GetString());
        Assert.Equal("jane@acme.example", item.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Stores_every_field_given()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/recruiters", new
        {
            name = "John Smith",
            email = "john@acme.example",
            phone = "+32 470 12 34 56",
            linkedInUrl = "https://www.linkedin.com/in/johnsmith"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/recruiters")).GetProperty("items").EnumerateArray());
        Assert.Equal("+32 470 12 34 56", item.GetProperty("phone").GetString());
        Assert.Equal("https://www.linkedin.com/in/johnsmith", item.GetProperty("linkedInUrl").GetString());
    }

    [Theory]
    [InlineData("{\"name\":\"Jane Doe\",\"email\":\"\",\"phone\":\"\",\"linkedInUrl\":\"\"}")]
    [InlineData("{\"name\":\"Jane Doe\",\"email\":\"  \",\"phone\":\" \",\"linkedInUrl\":\"  \"}")]
    [InlineData("{\"name\":\"Jane Doe\"}")]
    [InlineData("{\"name\":\"Jane Doe\",\"email\":null,\"phone\":null,\"linkedInUrl\":null}")]
    public async Task Empty_or_missing_optional_fields_are_treated_as_not_given(string json)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsync("/recruiters", new StringContent(json, new MediaTypeHeaderValue("application/json")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/recruiters")).GetProperty("items").EnumerateArray());
        Assert.Equal("Jane Doe", item.GetProperty("name").GetString());
        foreach (var field in new[] { "email", "phone", "linkedInUrl" })
        {
            Assert.True(!item.TryGetProperty(field, out var value) || value.ValueKind == JsonValueKind.Null);
        }
    }

    public static TheoryData<string, object, string> InvalidInputs => new()
    {
        { "no name", new { email = "jane@acme.example" }, "name" },
        { "an empty name", new { name = "" }, "name" },
        { "a name of only spaces and tabs", new { name = "  \t \t " }, "name" },
        { "a 201-character name", new { name = new string('a', 201) }, "name" },
        { "email not-an-email", new { name = "Jane", email = "not-an-email" }, "email" },
        { "an overlong email", new { name = "Jane", email = new string('a', 250) + "@x.io" }, "email" },
        { "LinkedIn URL not absolute", new { name = "Jane", linkedInUrl = "linkedin.com/in/x" }, "linkedInUrl" },
        { "LinkedIn URL with ftp scheme", new { name = "Jane", linkedInUrl = "ftp://example.com/x" }, "linkedInUrl" },
        { "an overlong LinkedIn URL", new { name = "Jane", linkedInUrl = "https://example.com/" + new string('a', 2048) }, "linkedInUrl" },
        { "a 51-character phone", new { name = "Jane", phone = new string('1', 51) }, "phone" },
    };

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public async Task Invalid_input_is_a_400_problem_and_stores_nothing(string description, object body, string field)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var before = await TotalAsync(client);

        var response = await client.PostAsJsonAsync("/recruiters", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out _), $"{description}: expected an error under errors.{field}");
        Assert.Equal(before, await TotalAsync(client));
    }

    [Fact]
    public async Task Limits_are_inclusive()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/recruiters", new
        {
            name = new string('a', 200),
            email = new string('a', 254 - "@acme.example".Length) + "@acme.example",
            phone = new string('1', 50),
            linkedInUrl = "https://example.com/" + new string('a', 2048 - "https://example.com/".Length)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Reports_every_invalid_field_together()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/recruiters", new { name = "", email = "not-an-email" });

        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(["email", "name"], errors.EnumerateObject().Select(property => property.Name).Order());
    }

    [Fact]
    public async Task Recruiters_are_private_to_their_owner()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateAsync(userA, "Xavier");
        await CreateAsync(userB, "Zoe");

        var list = await userB.GetFromJsonAsync<JsonElement>("/recruiters");

        Assert.Equal(["Zoe"], Names(list));
        Assert.Equal(1, list.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task An_owner_in_the_request_is_ignored()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        var userAId = await AuthenticatedClient.GetUserIdAsync(userA);

        var response = await userB.PostAsJsonAsync("/recruiters", new { name = "Zoe", ownerId = userAId, userId = userAId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(["Zoe"], Names(await userB.GetFromJsonAsync<JsonElement>("/recruiters")));
        var forA = await userA.GetFromJsonAsync<JsonElement>("/recruiters");
        Assert.Empty(Names(forA));
        Assert.Equal(0, forA.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Anonymous_requests_get_a_401_problem_and_store_nothing(string method)
    {
        using var signedIn = await AuthenticatedClient.CreateSignedInAsync(factory);
        var before = await TotalAsync(signedIn);
        using var anonymous = factory.CreateClient();

        var response = await anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/recruiters")
        {
            Content = method == "POST" ? JsonContent.Create(new { name = "Nobody" }) : null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(before, await TotalAsync(signedIn));
    }

    [Fact]
    public async Task Lists_are_paged_oldest_first_like_bureaus()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var other = await AuthenticatedClient.CreateSignedInAsync(factory);
        foreach (var name in new[] { "one", "two", "three", "four", "five" })
        {
            await CreateAsync(owner, name);
        }
        await CreateAsync(other, "foreign");

        var first = await owner.GetFromJsonAsync<JsonElement>("/recruiters?page=1&pageSize=2");
        var last = await owner.GetFromJsonAsync<JsonElement>("/recruiters?page=3&pageSize=2");
        var beyond = await owner.GetFromJsonAsync<JsonElement>("/recruiters?page=4&pageSize=2");
        var defaults = await owner.GetFromJsonAsync<JsonElement>("/recruiters");

        Assert.Equal(["one", "two"], Names(first));
        Assert.Equal(["five"], Names(last));
        Assert.Empty(Names(beyond));
        Assert.All(new[] { first, last, beyond, defaults }, page => Assert.Equal(5, page.GetProperty("total").GetInt32()));
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

        var response = await client.GetAsync($"/recruiters?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(field, Assert.Single(errors.EnumerateObject()).Name);
    }

    private static string?[] Names(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToArray();

    private static async Task<int> TotalAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/recruiters")).GetProperty("total").GetInt32();

    private static async Task CreateAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/recruiters", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
