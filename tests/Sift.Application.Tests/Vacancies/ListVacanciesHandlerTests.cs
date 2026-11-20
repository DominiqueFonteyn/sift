using Sift.Application.Common;
using Sift.Application.Matching;
using Sift.Application.Requirements;
using Sift.Application.Resumes;
using Sift.Application.Vacancies;
using Sift.Domain.Matching;
using Sift.Domain.Requirements;

namespace Sift.Application.Tests.Vacancies;

public sealed class ListVacanciesHandlerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid me = Guid.NewGuid();
    private readonly Guid someoneElse = Guid.NewGuid();
    private readonly List<(Guid Owner, VacancyDetail Vacancy)> stored = [];
    private readonly HashSet<Guid> resumeOwners = [];
    private readonly List<(Guid Owner, string Text, RequirementKind Kind)> requirementRows = [];
    private readonly CountingEngine engine = new();
    private readonly ListVacanciesHandler handler;

    public ListVacanciesHandlerTests() =>
        handler = new ListVacanciesHandler(new Vacancies(stored), new Resumes(resumeOwners), new Requirements(requirementRows), engine);

    private static PageRequest Page(string? page = null, string? size = null)
    {
        Assert.True(PageRequest.TryCreate(page, size, out var request, out _));

        return request;
    }

    [Fact]
    public async Task Ranks_all_owned_vacancies_before_paging()
    {
        resumeOwners.Add(me);
        requirementRows.Add((me, "C#", RequirementKind.MustHave));
        requirementRows.Add((me, "SQL", RequirementKind.NiceToHave));
        Add(me, "Alpha", "C# SQL", 0);
        Add(me, "Bravo", "Java", 1);
        Add(me, "Charlie", "C#", 2);

        var first = await handler.HandleAsync(new ListVacanciesQuery(me, Page("1", "2")), CancellationToken.None);
        var second = await handler.HandleAsync(new ListVacanciesQuery(me, Page("2", "2")), CancellationToken.None);
        var beyond = await handler.HandleAsync(new ListVacanciesQuery(me, Page("5", "2")), CancellationToken.None);

        Assert.Equal(["Alpha", "Charlie"], first.Page.Items.Select(i => i.Title).ToArray());
        Assert.Equal(["Bravo"], second.Page.Items.Select(i => i.Title).ToArray());
        Assert.Equal(3, first.Page.Total);
        Assert.Empty(beyond.Page.Items);
        Assert.Equal(3, beyond.Page.Total);
        Assert.Empty(first.Missing);
    }

    [Theory]
    [InlineData(false, true, new[] { MatchInput.Resume })]
    [InlineData(true, false, new[] { MatchInput.Requirements })]
    [InlineData(false, false, new[] { MatchInput.Resume, MatchInput.Requirements })]
    public async Task Without_inputs_nothing_is_scored_and_the_list_is_newest_first(bool hasResume, bool hasRequirements, MatchInput[] expectedMissing)
    {
        if (hasResume)
        {
            resumeOwners.Add(me);
        }

        if (hasRequirements)
        {
            requirementRows.Add((me, "C#", RequirementKind.MustHave));
        }

        Add(me, "First", "C#", 0);
        Add(me, "Second", "C#", 1);
        Add(me, "Third", "C#", 2);

        var result = await handler.HandleAsync(new ListVacanciesQuery(me, Page()), CancellationToken.None);

        Assert.Equal(["Third", "Second", "First"], result.Page.Items.Select(i => i.Title).ToArray());
        Assert.All(result.Page.Items, item => Assert.Null(item.Match));
        Assert.Equal(expectedMissing, result.Missing.ToArray());
        Assert.Equal(0, engine.Calls);
    }

    [Fact]
    public async Task Other_users_vacancies_inputs_and_requirements_are_never_used()
    {
        resumeOwners.Add(me);
        resumeOwners.Add(someoneElse);
        requirementRows.Add((me, "C#", RequirementKind.MustHave));
        requirementRows.Add((someoneElse, "Kotlin", RequirementKind.MustHave));
        Add(me, "Mine", "C#", 0);
        Add(someoneElse, "Foreign", "C# Kotlin", 1);

        var result = await handler.HandleAsync(new ListVacanciesQuery(me, Page()), CancellationToken.None);

        var item = Assert.Single(result.Page.Items);
        Assert.Equal("Mine", item.Title);
        Assert.Equal(1, result.Page.Total);
        Assert.Equal("C#", Assert.Single(item.Match!.Verdicts).Term);
        Assert.Equal(1, engine.Calls);
    }

    [Fact]
    public async Task Another_users_resume_does_not_count()
    {
        resumeOwners.Add(someoneElse);
        requirementRows.Add((me, "C#", RequirementKind.MustHave));
        Add(me, "Mine", "C#", 0);

        var result = await handler.HandleAsync(new ListVacanciesQuery(me, Page()), CancellationToken.None);

        Assert.Equal([MatchInput.Resume], result.Missing.ToArray());
        Assert.Null(Assert.Single(result.Page.Items).Match);
    }

    private void Add(Guid owner, string title, string text, int minutes) =>
        stored.Add((owner, new VacancyDetail(Guid.NewGuid(), title, "Co", text, T0.AddMinutes(minutes))));

    private sealed class Vacancies(List<(Guid Owner, VacancyDetail Vacancy)> stored) : IVacancyQueries
    {
        public Task<IReadOnlyList<VacancyDetail>> ListAllAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VacancyDetail>>(stored.Where(s => s.Owner == ownerId).Select(s => s.Vacancy).ToList());

        public Task<VacancyDetail?> GetAsync(Guid ownerId, Guid vacancyId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class Resumes(HashSet<Guid> owners) : IResumeQueries
    {
        public Task<ResumeReadModel?> GetAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult(owners.Contains(ownerId) ? new ResumeReadModel("Resume", T0) : null);
    }

    private sealed class Requirements(List<(Guid Owner, string Text, RequirementKind Kind)> rows) : IRequirementQueries
    {
        public Task<PagedResult<RequirementReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<RequirementReadModel>> ListAllAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RequirementReadModel>>(
                rows.Where(r => r.Owner == ownerId).Select(r => new RequirementReadModel(Guid.NewGuid(), r.Text, r.Kind, T0)).ToList());
    }

    private sealed class CountingEngine : IMatchEngine
    {
        public int Calls { get; private set; }

        public Task<MatchResult> MatchAsync(string vacancyText, IReadOnlyList<MatchRequirement> requirements, CancellationToken cancellationToken)
        {
            Calls++;

            return Task.FromResult(VacancyScorer.Score(vacancyText, requirements));
        }
    }
}
