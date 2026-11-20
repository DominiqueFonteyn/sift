using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Sift.Api.IntegrationTests.Vacancies;

public sealed class VacancyEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private static readonly DateTimeOffset Start = new(2026, 3, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Creating_a_vacancy_trims_title_and_company_and_returns_it_first_in_the_list()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(Start);

        try
        {
            var response = await client.PostAsJsonAsync("/vacancies", new { title = "  Backend Engineer ", company = "Acme", rawText = "We need C# and SQL" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
            Assert.Equal("Backend Engineer", body.GetProperty("title").GetString());
            Assert.Equal("Acme", body.GetProperty("company").GetString());
            Assert.Equal("We need C# and SQL", body.GetProperty("rawText").GetString());
            Assert.Equal(Start, body.GetProperty("createdAt").GetDateTimeOffset());
            Assert.Equal(TimeSpan.Zero, body.GetProperty("createdAt").GetDateTimeOffset().Offset);

            var list = await client.GetFromJsonAsync<JsonElement>("/vacancies");
            var first = list.GetProperty("items")[0];
            Assert.Equal(body.GetProperty("id").GetGuid(), first.GetProperty("id").GetGuid());
        }
        finally
        {
            factory.Clock.Reset();
        }
    }

    [Fact]
    public async Task Raw_text_is_stored_exactly_as_sent()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        const string text = "  line one\r\n\tline two  \n";

        var response = await client.PostAsJsonAsync("/vacancies", new { title = "T", company = "C", rawText = text });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(text, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("rawText").GetString());
    }

    [Fact]
    public async Task Id_and_created_at_sent_in_the_body_are_ignored()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var forgedId = Guid.NewGuid();
        factory.Clock.Set(Start);

        try
        {
            var response = await client.PostAsJsonAsync("/vacancies", new { id = forgedId, createdAt = "1999-01-01T00:00:00Z", title = "T", company = "C", rawText = "text" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(forgedId, body.GetProperty("id").GetGuid());
            Assert.Equal(Start, body.GetProperty("createdAt").GetDateTimeOffset());
        }
        finally
        {
            factory.Clock.Reset();
        }
    }

    public static TheoryData<string, string?, bool> InvalidFields =>
        new()
        {
            { "title", null, true },
            { "title", "", false },
            { "title", "   ", false },
            { "company", null, true },
            { "company", "", false },
            { "company", "\t\n ", false },
            { "rawText", null, true },
            { "rawText", "", false },
            { "rawText", "   ", false }
        };

    [Theory]
    [MemberData(nameof(InvalidFields))]
    public async Task A_missing_or_blank_field_is_a_400_problem_keyed_by_field_and_nothing_is_stored(string field, string? value, bool omit)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var body = new Dictionary<string, string?> { ["title"] = "T", ["company"] = "C", ["rawText"] = "text" };
        if (omit)
        {
            body.Remove(field);
        }
        else
        {
            body[field] = value;
        }

        var response = await client.PostAsJsonAsync("/vacancies", body);

        await AssertValidationProblem(response, field);
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/vacancies")).GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("title", 200, HttpStatusCode.Created)]
    [InlineData("title", 201, HttpStatusCode.BadRequest)]
    [InlineData("company", 200, HttpStatusCode.Created)]
    [InlineData("company", 201, HttpStatusCode.BadRequest)]
    [InlineData("rawText", 20000, HttpStatusCode.Created)]
    [InlineData("rawText", 20001, HttpStatusCode.BadRequest)]
    public async Task Length_limits_apply_to_the_value_as_sent(string field, int length, HttpStatusCode expected)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var body = new Dictionary<string, string> { ["title"] = "T", ["company"] = "C", ["rawText"] = "text" };
        body[field] = new string('a', length);

        var response = await client.PostAsJsonAsync("/vacancies", body);

        Assert.Equal(expected, response.StatusCode);
        var total = (await client.GetFromJsonAsync<JsonElement>("/vacancies")).GetProperty("total").GetInt32();
        if (expected == HttpStatusCode.BadRequest)
        {
            await AssertValidationProblem(response, field);
            Assert.Equal(0, total);
        }
        else
        {
            Assert.Equal(1, total);
        }
    }

    [Fact]
    public async Task Whitespace_padding_counts_towards_the_title_limit_as_sent()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PostAsJsonAsync("/vacancies", new { title = " " + new string('a', 200), company = "C", rawText = "text" });

        await AssertValidationProblem(response, "title");
    }

    [Fact]
    public async Task The_list_is_newest_first_and_paged_with_a_page_shape_without_raw_text()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateManyAsync(client, 25);

        var page = await client.GetFromJsonAsync<JsonElement>("/vacancies?page=2&pageSize=10");

        Assert.Equal(25, page.GetProperty("total").GetInt32());
        Assert.Equal(2, page.GetProperty("page").GetInt32());
        Assert.Equal(10, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(Enumerable.Range(6, 10).Reverse().Select(i => $"V{i:00}").ToArray(), Titles(page));
        foreach (var item in page.GetProperty("items").EnumerateArray())
        {
            Assert.Equal(["company", "createdAt", "id", "title"], item.EnumerateObject().Select(p => p.Name).Order().ToArray());
        }
    }

    [Fact]
    public async Task Paging_defaults_to_page_one_of_twenty_and_a_page_beyond_the_end_is_empty()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateManyAsync(client, 25);

        var defaults = await client.GetFromJsonAsync<JsonElement>("/vacancies");
        var beyond = await client.GetFromJsonAsync<JsonElement>("/vacancies?page=4&pageSize=10");

        Assert.Equal(1, defaults.GetProperty("page").GetInt32());
        Assert.Equal(20, defaults.GetProperty("pageSize").GetInt32());
        Assert.Equal(25, defaults.GetProperty("total").GetInt32());
        Assert.Equal("V25", Titles(defaults).First());
        Assert.Equal(20, Titles(defaults).Length);
        Assert.Empty(Titles(beyond));
        Assert.Equal(25, beyond.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Vacancies_created_at_the_same_instant_are_ordered_deterministically_without_gaps_or_repeats()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(Start);

        try
        {
            for (var i = 0; i < 6; i++)
            {
                await CreateAsync(client, $"Same{i}");
            }
        }
        finally
        {
            factory.Clock.Reset();
        }

        var all = Titles(await client.GetFromJsonAsync<JsonElement>("/vacancies?pageSize=100"));
        var paged = Titles(await client.GetFromJsonAsync<JsonElement>("/vacancies?page=1&pageSize=4"))
            .Concat(Titles(await client.GetFromJsonAsync<JsonElement>("/vacancies?page=2&pageSize=4")))
            .ToArray();

        Assert.Equal(6, all.Distinct().Count());
        Assert.Equal(all, paged);
    }

    [Fact]
    public async Task An_empty_list_has_a_zero_total_and_no_items()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var page = await client.GetFromJsonAsync<JsonElement>("/vacancies");

        Assert.Equal(0, page.GetProperty("total").GetInt32());
        Assert.Empty(Titles(page));
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("page=-1", "page")]
    [InlineData("page=abc", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("pageSize=abc", "pageSize")]
    public async Task Invalid_paging_is_a_400_problem_keyed_by_the_parameter(string query, string field)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.GetAsync($"/vacancies?{query}");

        await AssertValidationProblem(response, field);
    }

    [Fact]
    public async Task Listing_never_shows_another_users_vacancies_and_counts_only_the_callers()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        var secret = await CreateAsync(userA, "A's secret role");
        await CreateAsync(userB, "B1");
        await CreateAsync(userB, "B2");
        await CreateAsync(userB, "B3");

        var page = await userB.GetFromJsonAsync<JsonElement>("/vacancies?page=1&pageSize=100");

        Assert.Equal(3, page.GetProperty("total").GetInt32());
        Assert.Equal(new[] { "B3", "B2", "B1" }, Titles(page));
        Assert.DoesNotContain(page.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == secret);
    }

    [Fact]
    public async Task A_supplied_owner_id_or_user_id_is_ignored_in_favour_of_the_token()
    {
        using var attacker = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var victim = await AuthenticatedClient.CreateSignedInAsync(factory);
        var victimId = await AuthenticatedClient.GetUserIdAsync(victim);

        var response = await attacker.PostAsJsonAsync("/vacancies", new { title = "Spoofed", company = "C", rawText = "text", ownerId = victimId, userId = victimId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var attackerList = await attacker.GetFromJsonAsync<JsonElement>("/vacancies");
        var victimList = await victim.GetFromJsonAsync<JsonElement>("/vacancies");
        Assert.Equal(id, attackerList.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(0, victimList.GetProperty("total").GetInt32());
        Assert.Empty(Titles(victimList));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Without_a_token_the_request_is_a_401_problem(string method)
    {
        using var client = factory.CreateClient();

        var response = await Send(client, method);

        await AssertUnauthorized(response);
    }

    [Theory]
    [InlineData("GET", "malformed")]
    [InlineData("POST", "malformed")]
    [InlineData("GET", "expired")]
    [InlineData("POST", "expired")]
    public async Task A_malformed_or_expired_token_is_a_401_problem_and_creates_nothing(string method, string kind)
    {
        using var signedIn = await AuthenticatedClient.CreateSignedInAsync(factory);
        var userId = await AuthenticatedClient.GetUserIdAsync(signedIn);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", kind == "malformed" ? "not-a-jwt" : ExpiredToken(userId));

        var response = await Send(client, method);

        await AssertUnauthorized(response);
        Assert.Equal(0, (await signedIn.GetFromJsonAsync<JsonElement>("/vacancies")).GetProperty("total").GetInt32());
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string method) =>
        client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/vacancies")
        {
            Content = method == "POST" ? JsonContent.Create(new { title = "T", company = "C", rawText = "text" }) : null
        });

    private static async Task AssertUnauthorized(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(401, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());
    }

    private static async Task AssertValidationProblem(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(field, Assert.Single(errors.EnumerateObject()).Name);
    }

    private static string[] Titles(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("title").GetString()!).ToArray();

    private static async Task<Guid> CreateAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/vacancies", new { title, company = "Acme", rawText = "text" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task CreateManyAsync(HttpClient client, int count)
    {
        try
        {
            for (var i = 1; i <= count; i++)
            {
                factory.Clock.Set(Start.AddMinutes(i));
                await CreateAsync(client, $"V{i:00}");
            }
        }
        finally
        {
            factory.Clock.Reset();
        }
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
