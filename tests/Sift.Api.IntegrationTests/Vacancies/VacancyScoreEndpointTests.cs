using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sift.Application.Matching;
using Sift.Domain.Matching;

namespace Sift.Api.IntegrationTests.Vacancies;

public sealed class VacancyScoreEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private const string MustHave = "must-have";

    private const string NiceToHave = "nice-to-have";

    [Fact]
    public async Task Opening_a_vacancy_shows_an_integer_score_and_a_verdict_with_an_explanation_per_requirement()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await SaveResumeAsync(client);
        await AddRequirementAsync(client, "C#", MustHave);
        await AddRequirementAsync(client, "Docker", NiceToHave);
        var id = await CreateVacancyAsync(client, "We need C# and SQL");

        var match = await GetMatchAsync(client, id);

        var score = match.GetProperty("score");
        Assert.Equal(JsonValueKind.Number, score.ValueKind);
        Assert.InRange(score.GetInt32(), 0, 100);
        Assert.Empty(match.GetProperty("missingInputs").EnumerateArray());
        var verdicts = match.GetProperty("verdicts").EnumerateArray().ToList();
        Assert.Equal(2, verdicts.Count);
        var csharp = verdicts.Single(v => v.GetProperty("requirement").GetString() == "C#");
        Assert.Equal("met", csharp.GetProperty("verdict").GetString());
        Assert.Equal(MustHave, csharp.GetProperty("kind").GetString());
        Assert.Contains("\"C#\"", csharp.GetProperty("explanation").GetString());
        Assert.Contains("was found", csharp.GetProperty("explanation").GetString());
        var docker = verdicts.Single(v => v.GetProperty("requirement").GetString() == "Docker");
        Assert.Equal("not met", docker.GetProperty("verdict").GetString());
        Assert.Equal(NiceToHave, docker.GetProperty("kind").GetString());
        Assert.Contains("\"Docker\"", docker.GetProperty("explanation").GetString());
        Assert.Contains("was not found", docker.GetProperty("explanation").GetString());
    }

    [Fact]
    public async Task An_unmet_must_have_caps_the_score_and_is_listed_first()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await SaveResumeAsync(client);
        await AddRequirementAsync(client, "SQL", NiceToHave);
        await AddRequirementAsync(client, "C#", MustHave);
        await AddRequirementAsync(client, "Docker", NiceToHave);
        await AddRequirementAsync(client, "Remote", MustHave);
        var id = await CreateVacancyAsync(client, "C# SQL Docker, on-site only");

        var match = await GetMatchAsync(client, id);

        Assert.True(match.GetProperty("score").GetInt32() <= 49);
        var first = match.GetProperty("verdicts")[0];
        Assert.Equal("Remote", first.GetProperty("requirement").GetString());
        Assert.Equal(MustHave, first.GetProperty("kind").GetString());
        Assert.Equal("not met", first.GetProperty("verdict").GetString());
    }

    [Fact]
    public async Task Meeting_every_must_have_reaches_the_pass_threshold()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await SaveResumeAsync(client);
        await AddRequirementAsync(client, "C#", MustHave);
        await AddRequirementAsync(client, "Docker", NiceToHave);
        var id = await CreateVacancyAsync(client, "We need C#");

        var match = await GetMatchAsync(client, id);

        Assert.True(match.GetProperty("score").GetInt32() >= 50);
    }

    [Fact]
    public async Task Meeting_more_nice_to_haves_scores_higher()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await SaveResumeAsync(client);
        await AddRequirementAsync(client, "C#", MustHave);
        await AddRequirementAsync(client, "SQL", NiceToHave);
        await AddRequirementAsync(client, "Docker", NiceToHave);
        await AddRequirementAsync(client, "Azure", NiceToHave);
        var x = await CreateVacancyAsync(client, "C# SQL Docker");
        var y = await CreateVacancyAsync(client, "C# SQL");

        var scoreX = (await GetMatchAsync(client, x)).GetProperty("score").GetInt32();
        var scoreY = (await GetMatchAsync(client, y)).GetProperty("score").GetInt32();

        Assert.True(scoreX > scoreY, $"{scoreX} should be greater than {scoreY}");
    }

    [Fact]
    public async Task Scoring_the_same_vacancy_twice_gives_identical_results()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await SaveResumeAsync(client);
        await AddRequirementAsync(client, "C#", MustHave);
        await AddRequirementAsync(client, "Remote", MustHave);
        await AddRequirementAsync(client, "Docker", NiceToHave);
        var id = await CreateVacancyAsync(client, "C# on-site");

        var first = (await GetMatchAsync(client, id)).GetRawText();
        var second = (await GetMatchAsync(client, id)).GetRawText();

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(false, true, new[] { "resume" })]
    [InlineData(true, false, new[] { "requirements" })]
    [InlineData(false, false, new[] { "resume", "requirements" })]
    public async Task Without_a_resume_or_requirements_there_is_no_score_and_the_missing_inputs_are_listed(
        bool hasResume, bool hasRequirements, string[] expectedMissing)
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        if (hasResume)
        {
            await SaveResumeAsync(client);
        }

        if (hasRequirements)
        {
            await AddRequirementAsync(client, "C#", MustHave);
        }

        var id = await CreateVacancyAsync(client, "We need C#");

        var match = await GetMatchAsync(client, id);

        Assert.Equal(JsonValueKind.Null, match.GetProperty("score").ValueKind);
        Assert.Empty(match.GetProperty("verdicts").EnumerateArray());
        Assert.Equal(expectedMissing, match.GetProperty("missingInputs").EnumerateArray().Select(m => m.GetString()).ToArray());
    }

    [Fact]
    public async Task Replacing_a_requirement_re_scores_the_vacancy_on_the_next_open()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        await SaveResumeAsync(client);
        var javaId = await AddRequirementAsync(client, "Java", MustHave);
        var id = await CreateVacancyAsync(client, "We need C#");

        var before = await GetMatchAsync(client, id);
        Assert.Equal("not met", Assert.Single(before.GetProperty("verdicts").EnumerateArray()).GetProperty("verdict").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/requirements/{javaId}")).StatusCode);
        await AddRequirementAsync(client, "C#", MustHave);
        var after = await GetMatchAsync(client, id);

        var verdict = Assert.Single(after.GetProperty("verdicts").EnumerateArray());
        Assert.Equal("C#", verdict.GetProperty("requirement").GetString());
        Assert.Equal("met", verdict.GetProperty("verdict").GetString());
        Assert.DoesNotContain("Java", after.GetRawText());
        Assert.True(after.GetProperty("score").GetInt32() >= 50);
    }

    [Fact]
    public async Task Only_my_own_requirements_are_used()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        await SaveResumeAsync(userA);
        await SaveResumeAsync(userB);
        await AddRequirementAsync(userA, "C#", MustHave);
        await AddRequirementAsync(userB, "Kotlin", MustHave);
        var id = await CreateVacancyAsync(userA, "C# and Kotlin");

        var match = await GetMatchAsync(userA, id);

        var verdict = Assert.Single(match.GetProperty("verdicts").EnumerateArray());
        Assert.Equal("C#", verdict.GetProperty("requirement").GetString());
        Assert.Equal("met", verdict.GetProperty("verdict").GetString());
        Assert.DoesNotContain("Kotlin", match.GetRawText());
    }

    [Fact]
    public async Task Another_users_resume_does_not_count()
    {
        using var userA = await AuthenticatedClient.CreateSignedInAsync(factory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(factory);
        await AddRequirementAsync(userA, "C#", MustHave);
        await SaveResumeAsync(userB);
        var id = await CreateVacancyAsync(userA, "We need C#");

        var match = await GetMatchAsync(userA, id);

        Assert.Equal(JsonValueKind.Null, match.GetProperty("score").ValueKind);
        Assert.Empty(match.GetProperty("verdicts").EnumerateArray());
        Assert.Equal(["resume"], match.GetProperty("missingInputs").EnumerateArray().Select(m => m.GetString()!).ToArray());
    }

    [Fact]
    public async Task Another_users_vacancy_is_a_404_problem_with_no_score_and_the_matcher_is_not_called()
    {
        var matcher = new RecordingMatchEngine();
        using var recordingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMatchEngine>();
            services.AddSingleton<IMatchEngine>(matcher);
        }));
        using var userA = await AuthenticatedClient.CreateSignedInAsync(recordingFactory);
        using var userB = await AuthenticatedClient.CreateSignedInAsync(recordingFactory);
        await SaveResumeAsync(userA);
        await AddRequirementAsync(userA, "C#", MustHave);
        var foreignId = await CreateVacancyAsync(userB, "We need C#");

        var response = await userA.GetAsync($"/vacancies/{foreignId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(404, JsonDocument.Parse(body).RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("score", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("verdict", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("explanation", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, matcher.CallCount);

        var own = await CreateVacancyAsync(userA, "We need C#");
        Assert.Equal(HttpStatusCode.OK, (await userA.GetAsync($"/vacancies/{own}")).StatusCode);
        Assert.Equal(1, matcher.CallCount);
    }

    [Fact]
    public async Task Without_a_token_there_is_a_401_problem_and_no_score()
    {
        using var owner = await AuthenticatedClient.CreateSignedInAsync(factory);
        await SaveResumeAsync(owner);
        await AddRequirementAsync(owner, "C#", MustHave);
        var id = await CreateVacancyAsync(owner, "We need C#");
        using var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync($"/vacancies/{id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("score", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("verdict", body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SaveResumeAsync(HttpClient client)
    {
        var response = await client.PutAsJsonAsync("/resume", new { text = "Ten years of backend work." });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> AddRequirementAsync(HttpClient client, string text, string kind)
    {
        var response = await client.PostAsJsonAsync("/requirements", new { text, kind });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateVacancyAsync(HttpClient client, string rawText)
    {
        var response = await client.PostAsJsonAsync("/vacancies", new { title = "Backend Engineer", company = "Acme", rawText });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> GetMatchAsync(HttpClient client, Guid vacancyId)
    {
        var response = await client.GetAsync($"/vacancies/{vacancyId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("match");
    }

    private sealed class RecordingMatchEngine : IMatchEngine
    {
        private int callCount;

        public int CallCount => Volatile.Read(ref callCount);

        public Task<MatchResult> MatchAsync(string vacancyText, IReadOnlyList<MatchRequirement> requirements, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref callCount);

            return Task.FromResult(VacancyScorer.Score(vacancyText, requirements));
        }
    }
}
