using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sift.Api.IntegrationTests.Resumes;

public sealed class ResumeEndpointTests(SiftApiFactory fixture) : IClassFixture<SiftApiFactory>
{
    private static readonly DateTimeOffset T1 = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = T1.AddHours(3);

    // Tokens are stamped by the application clock but validated in real time, so every test signs in on the real clock and pins it afterwards.
    private readonly SiftApiFactory factory = ResetClock(fixture);

    private static SiftApiFactory ResetClock(SiftApiFactory fixture)
    {
        fixture.Clock.Reset();
        return fixture;
    }

    private static Task<HttpResponseMessage> SaveAsync(HttpClient client, string? text) =>
        client.PutAsJsonAsync("/resume", new { text });

    private static async Task AssertResumeAsync(HttpClient client, string text, DateTimeOffset updatedAt)
    {
        var response = await client.GetAsync("/resume");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(text, body.GetProperty("text").GetString());
        Assert.Equal(updatedAt, body.GetProperty("updatedAt").GetDateTimeOffset());
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Get_returns_a_404_problem_when_the_user_has_no_resume()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        await AssertProblemAsync(await client.GetAsync("/resume"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task First_save_succeeds_and_get_returns_the_text_with_the_clock_time()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(T1);

        var save = await SaveAsync(client, "My first resume");

        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        await AssertResumeAsync(client, "My first resume", T1);
    }

    [Fact]
    public async Task Saving_again_replaces_the_text_and_moves_updated_at_to_the_new_clock_time()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(T1);
        await SaveAsync(client, "v1");
        factory.Clock.Set(T2);

        var save = await SaveAsync(client, "v2");

        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        await AssertResumeAsync(client, "v2", T2);
    }

    [Fact]
    public async Task Exactly_20000_characters_are_saved_and_returned_unchanged()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(T1);
        var text = new string('a', 19_999) + "\u00e9";

        Assert.Equal(HttpStatusCode.OK, (await SaveAsync(client, text)).StatusCode);

        await AssertResumeAsync(client, text, T1);
    }

    [Fact]
    public async Task Leading_and_trailing_whitespace_is_stored_as_submitted()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(T1);
        const string text = "  indented\r\n\r\n  line  \n";

        await SaveAsync(client, text);

        await AssertResumeAsync(client, text, T1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t\r\n ")]
    public async Task Blank_text_returns_a_400_problem_and_keeps_the_previous_resume(string blank)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(T1);
        await SaveAsync(client, "keep me");
        factory.Clock.Set(T2);

        var response = await SaveAsync(client, blank);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertResumeAsync(client, "keep me", T1);
    }

    [Fact]
    public async Task Missing_text_returns_a_400_problem()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        var response = await client.PutAsJsonAsync("/resume", new { });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Text_of_20001_characters_returns_a_400_problem_naming_the_limit_and_keeps_the_previous_resume()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(T1);
        await SaveAsync(client, "keep me");
        factory.Clock.Set(T2);

        var response = await SaveAsync(client, new string('a', 20_001));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("20,000", body);
        await AssertResumeAsync(client, "keep me", T1);
    }

    [Fact]
    public async Task A_rejected_first_save_leaves_get_returning_404()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);

        await AssertProblemAsync(await SaveAsync(client, "  "), HttpStatusCode.BadRequest);
        await AssertProblemAsync(await SaveAsync(client, new string('a', 20_001)), HttpStatusCode.BadRequest);

        await AssertProblemAsync(await client.GetAsync("/resume"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Another_users_resume_is_not_visible()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        await SaveAsync(userA, "A's resume");

        await AssertProblemAsync(await userB.GetAsync("/resume"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Saving_as_another_user_does_not_change_my_resume()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(T1);
        await SaveAsync(userA, "A's resume");
        factory.Clock.Set(T2);

        Assert.Equal(HttpStatusCode.OK, (await SaveAsync(userB, "B's resume")).StatusCode);

        await AssertResumeAsync(userA, "A's resume", T1);
        await AssertResumeAsync(userB, "B's resume", T2);
    }

    [Fact]
    public async Task A_user_id_in_the_body_is_ignored()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(T1);
        await SaveAsync(userA, "A's resume");
        var userAId = await AuthenticatedClient.GetUserIdAsync(userA);

        await userB.PutAsJsonAsync("/resume", new { text = "B's resume", ownerId = userAId, userId = userAId });

        await AssertResumeAsync(userA, "A's resume", T1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-valid-token")]
    public async Task Requests_without_a_valid_token_get_a_401_problem_and_change_nothing(string? token)
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        factory.Clock.Set(T1);
        await SaveAsync(owner, "existing");
        using var anonymous = factory.CreateClient();
        if (token is not null)
        {
            anonymous.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        factory.Clock.Set(T2);
        await AssertProblemAsync(await anonymous.GetAsync("/resume"), HttpStatusCode.Unauthorized);
        await AssertProblemAsync(await SaveAsync(anonymous, "overwritten"), HttpStatusCode.Unauthorized);

        await AssertResumeAsync(owner, "existing", T1);
    }
}
