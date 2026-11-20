using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sift.Api.IntegrationTests.Vacancies;

public sealed class VacancyRankingEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private const string MustHave = "must-have";
    private const string NiceToHave = "nice-to-have";

    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Default_ordering_is_by_score_descending_across_all_rows()
    {
        using var client = await SetUpAsync(true, ("C#", MustHave), ("SQL", NiceToHave), ("Docker", NiceToHave));
        await CreateAsync(client, "Alpha", "C# SQL Docker", 0);
        await CreateAsync(client, "Bravo", "Java only", 1);
        await CreateAsync(client, "Charlie", "C# SQL", 2);

        var page = await ListAsync(client);

        Assert.Equal(["Alpha", "Charlie", "Bravo"], Titles(page));
        var scores = Items(page).Select(i => i.GetProperty("score").GetInt32()).ToArray();
        Assert.Equal(scores.OrderByDescending(s => s).ToArray(), scores);
        Assert.Empty(page.GetProperty("missingInputs").EnumerateArray());
    }

    [Fact]
    public async Task Equal_scores_are_ordered_newest_first()
    {
        using var client = await SetUpAsync(true, ("C#", MustHave), ("SQL", NiceToHave));
        await CreateAsync(client, "Older", "C# SQL", 0);
        await CreateAsync(client, "Newer", "C# SQL", 5);

        var items = Items(await ListAsync(client));

        Assert.Equal(items[0].GetProperty("score").GetInt32(), items[1].GetProperty("score").GetInt32());
        Assert.Equal(["Newer", "Older"], items.Select(i => i.GetProperty("title").GetString()).ToArray());
    }

    [Fact]
    public async Task Equal_scores_and_equal_creation_time_are_ordered_by_lowercase_id_and_stay_stable()
    {
        using var client = await SetUpAsync(true, ("C#", MustHave));
        var one = await CreateAsync(client, "One", "C#", 0);
        var two = await CreateAsync(client, "Two", "C#", 0);
        var expected = new[] { one, two }.Select(id => id.ToString("D").ToLowerInvariant()).Order(StringComparer.Ordinal).ToArray();

        var first = Items(await ListAsync(client));
        var again = Items(await ListAsync(client));

        Assert.Equal(first[0].GetProperty("score").GetInt32(), first[1].GetProperty("score").GetInt32());
        Assert.Equal(expected, first.Select(i => i.GetProperty("id").GetString()).ToArray());
        Assert.Equal(expected, again.Select(i => i.GetProperty("id").GetString()).ToArray());
    }

    [Fact]
    public async Task Ranking_continues_across_pages_with_a_tie_at_the_boundary()
    {
        using var client = await SetUpAsync(true, ("C#", MustHave), ("SQL", NiceToHave), ("Docker", NiceToHave));
        await CreateAsync(client, "Alpha", "C# SQL Docker", 0);
        await CreateAsync(client, "Bravo", "C# SQL", 1);
        await CreateAsync(client, "Charlie", "C# SQL", 2);
        await CreateAsync(client, "Delta", "Java only", 3);

        var one = await ListAsync(client, "?page=1&pageSize=2");
        var two = await ListAsync(client, "?page=2&pageSize=2");

        Assert.Equal(["Alpha", "Charlie"], Titles(one));
        Assert.Equal(4, one.GetProperty("total").GetInt32());
        Assert.Equal(["Bravo", "Delta"], Titles(two));
        Assert.Equal(4, two.GetProperty("total").GetInt32());
        Assert.Equal(
            Items(one).Single(i => i.GetProperty("title").GetString() == "Charlie").GetProperty("score").GetInt32(),
            Items(two).Single(i => i.GetProperty("title").GetString() == "Bravo").GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task A_page_beyond_the_end_is_empty_with_the_correct_total()
    {
        using var client = await SetUpAsync(true, ("C#", MustHave));
        await CreateAsync(client, "Alpha", "C#", 0);
        await CreateAsync(client, "Bravo", "Java", 1);

        var page = await ListAsync(client, "?page=3&pageSize=2");

        Assert.Empty(Titles(page));
        Assert.Equal(2, page.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("pageSize=101", "pageSize")]
    public async Task Invalid_paging_is_a_400_problem_keyed_by_the_parameter(string query, string field)
    {
        using var client = await SetUpAsync(true, ("C#", MustHave));

        var response = await client.GetAsync($"/vacancies?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(field, Assert.Single((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").EnumerateObject()).Name);
    }

    [Fact]
    public async Task A_failed_must_have_is_flagged_named_capped_and_listed_first_in_the_verdicts()
    {
        using var client = await SetUpAsync(true, ("C#", NiceToHave), ("Remote", MustHave));
        await CreateAsync(client, "Onsite", "C# on-site only", 0);

        var item = Items(await ListAsync(client)).Single();

        Assert.True(item.GetProperty("failsMustHave").GetBoolean());
        Assert.Equal(["Remote"], item.GetProperty("unmetMustHaves").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.True(item.GetProperty("score").GetInt32() <= 49);
        var verdicts = item.GetProperty("verdicts").EnumerateArray().ToList();
        Assert.Equal("Remote", verdicts[0].GetProperty("requirement").GetString());
        Assert.Equal("not met", verdicts[0].GetProperty("verdict").GetString());
        Assert.Equal("C#", verdicts[1].GetProperty("requirement").GetString());
        Assert.Contains("was found", verdicts[1].GetProperty("explanation").GetString());
    }

    [Theory]
    [InlineData(false, true, new[] { "resume" })]
    [InlineData(true, false, new[] { "requirements" })]
    [InlineData(false, false, new[] { "resume", "requirements" })]
    public async Task Unscored_vacancies_are_newest_first_with_no_score_and_name_the_missing_inputs(bool hasResume, bool hasRequirements, string[] missing)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        if (hasResume)
        {
            (await client.PutAsJsonAsync("/resume", new { text = "Resume text" })).EnsureSuccessStatusCode();
        }

        if (hasRequirements)
        {
            await AddRequirementAsync(client, "C#", MustHave);
        }

        await CreateAsync(client, "First", "C#", 0);
        await CreateAsync(client, "Second", "C#", 1);
        await CreateAsync(client, "Third", "C#", 2);

        var page = await ListAsync(client);

        Assert.Equal(["Third", "Second", "First"], Titles(page));
        foreach (var item in Items(page))
        {
            Assert.Equal(JsonValueKind.Null, item.GetProperty("score").ValueKind);
            Assert.False(item.GetProperty("failsMustHave").GetBoolean());
            Assert.Empty(item.GetProperty("unmetMustHaves").EnumerateArray());
            Assert.Empty(item.GetProperty("verdicts").EnumerateArray());
        }

        Assert.Equal(missing, page.GetProperty("missingInputs").EnumerateArray().Select(e => e.GetString()).ToArray());
    }

    [Fact]
    public async Task The_ranking_uses_my_current_requirements()
    {
        using var client = await SetUpAsync(true);
        var java = await AddRequirementAsync(client, "Java", MustHave);
        await CreateAsync(client, "Old", "Java", 0);
        await CreateAsync(client, "New", "C#", 1);
        Assert.Equal(["Old", "New"], Titles(await ListAsync(client)));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/requirements/{java}")).StatusCode);
        await AddRequirementAsync(client, "C#", MustHave);

        Assert.Equal(["New", "Old"], Titles(await ListAsync(client)));
    }

    [Fact]
    public async Task Ownership_is_applied_before_ranking_and_paging()
    {
        using var userA = await SetUpAsync(true, ("Kotlin", MustHave));
        using var userB = await SetUpAsync(true, ("C#", MustHave), ("SQL", NiceToHave), ("Docker", NiceToHave));
        await CreateAsync(userB, "B-fit", "C# SQL", 0);
        await CreateAsync(userB, "B-miss", "Java", 1);
        await CreateAsync(userA, "A-foreign", "C# SQL Docker Kotlin", 2);

        var one = await ListAsync(userB, "?page=1&pageSize=1");
        var two = await ListAsync(userB, "?page=2&pageSize=1");

        Assert.Equal(["B-fit"], Titles(one));
        Assert.Equal(2, one.GetProperty("total").GetInt32());
        Assert.Equal(["B-miss"], Titles(two));
        var fit = Items(one).Single();
        Assert.True(fit.GetProperty("score").GetInt32() >= 50);
        Assert.False(fit.GetProperty("failsMustHave").GetBoolean());
        var miss = Items(two).Single();
        Assert.True(miss.GetProperty("failsMustHave").GetBoolean());
        Assert.Equal(["C#"], miss.GetProperty("unmetMustHaves").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.DoesNotContain("Kotlin", one.GetRawText() + two.GetRawText());
        Assert.DoesNotContain("A-foreign", one.GetRawText() + two.GetRawText());
    }

    [Fact]
    public async Task Another_users_scoring_inputs_are_never_borrowed()
    {
        using var userA = await SetUpAsync(true, ("C#", MustHave));
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        await CreateAsync(userB, "B-only", "C#", 0);

        var page = await ListAsync(userB);

        var item = Items(page).Single();
        Assert.Equal("B-only", item.GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("score").ValueKind);
        Assert.Equal(["resume", "requirements"], page.GetProperty("missingInputs").EnumerateArray().Select(e => e.GetString()).ToArray());
    }

    [Fact]
    public async Task Without_a_token_there_is_a_401_problem_and_no_vacancies()
    {
        using var owner = await SetUpAsync(true, ("C#", MustHave));
        await CreateAsync(owner, "Secret", "C#", 0);
        using var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/vacancies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Secret", await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpClient> SetUpAsync(bool resume, params (string Text, string Kind)[] requirements)
    {
        var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        if (resume)
        {
            (await client.PutAsJsonAsync("/resume", new { text = "Ten years of backend work." })).EnsureSuccessStatusCode();
        }

        foreach (var (text, kind) in requirements)
        {
            await AddRequirementAsync(client, text, kind);
        }

        return client;
    }

    private static async Task<Guid> AddRequirementAsync(HttpClient client, string text, string kind)
    {
        var response = await client.PostAsJsonAsync("/requirements", new { text, kind });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateAsync(HttpClient client, string title, string rawText, int minutes)
    {
        factory.Clock.Set(T0.AddMinutes(minutes));
        try
        {
            var response = await client.PostAsJsonAsync("/vacancies", new { title, company = "Acme", rawText });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }
        finally
        {
            factory.Clock.Reset();
        }
    }

    private static async Task<JsonElement> ListAsync(HttpClient client, string query = "") =>
        await client.GetFromJsonAsync<JsonElement>($"/vacancies{query}");

    private static List<JsonElement> Items(JsonElement page) => page.GetProperty("items").EnumerateArray().ToList();

    private static string[] Titles(JsonElement page) => Items(page).Select(i => i.GetProperty("title").GetString()!).ToArray();
}
