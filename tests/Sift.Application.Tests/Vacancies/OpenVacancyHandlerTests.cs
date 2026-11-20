using Sift.Application.Common;
using Sift.Application.Matching;
using Sift.Application.Requirements;
using Sift.Application.Resumes;
using Sift.Application.Vacancies;
using Sift.Domain.Matching;
using Sift.Domain.Requirements;

namespace Sift.Application.Tests.Vacancies;

public sealed class OpenVacancyHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly Guid me = Guid.NewGuid();
    private readonly Guid someoneElse = Guid.NewGuid();
    private readonly FakeVacancyQueries vacancies = new();
    private readonly FakeResumeQueries resumes = new();
    private readonly FakeRequirementQueries requirements = new();
    private readonly RecordingMatchEngine engine = new();
    private readonly OpenVacancyHandler handler;

    public OpenVacancyHandlerTests() => handler = new OpenVacancyHandler(vacancies, resumes, requirements, engine);

    [Fact]
    public async Task Scores_my_vacancy_against_my_own_requirements()
    {
        var vacancyId = AddVacancy(me, "We need C#");
        resumes.Add(me);
        requirements.Add(me, "C#", RequirementKind.MustHave);
        requirements.Add(me, "Docker", RequirementKind.NiceToHave);

        var result = await handler.HandleAsync(new OpenVacancyQuery(me, vacancyId), CancellationToken.None);

        var opened = Assert.IsType<OpenVacancyResult.Opened>(result);
        Assert.Equal(vacancyId, opened.Vacancy.Id);
        var scored = Assert.IsType<VacancyMatchOutcome.Scored>(opened.Match);
        Assert.Equal(50, scored.Result.Score);
        var call = Assert.Single(engine.Calls);
        Assert.Equal("We need C#", call.VacancyText);
        Assert.Equal(["C#", "Docker"], call.Requirements.Select(r => r.Term).ToArray());
    }

    [Fact]
    public async Task Another_users_vacancy_is_not_found_and_the_matcher_is_not_called()
    {
        var vacancyId = AddVacancy(someoneElse, "We need C#");
        resumes.Add(me);
        requirements.Add(me, "C#", RequirementKind.MustHave);

        var result = await handler.HandleAsync(new OpenVacancyQuery(me, vacancyId), CancellationToken.None);

        Assert.IsType<OpenVacancyResult.NotFound>(result);
        Assert.Empty(engine.Calls);
    }

    [Fact]
    public async Task An_unknown_vacancy_is_not_found_and_the_matcher_is_not_called()
    {
        resumes.Add(me);
        requirements.Add(me, "C#", RequirementKind.MustHave);

        var result = await handler.HandleAsync(new OpenVacancyQuery(me, Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<OpenVacancyResult.NotFound>(result);
        Assert.Empty(engine.Calls);
    }

    [Theory]
    [InlineData(false, true, new[] { MatchInput.Resume })]
    [InlineData(true, false, new[] { MatchInput.Requirements })]
    [InlineData(false, false, new[] { MatchInput.Resume, MatchInput.Requirements })]
    public async Task Names_each_missing_input_and_does_not_call_the_matcher(bool hasResume, bool hasRequirements, MatchInput[] expectedMissing)
    {
        var vacancyId = AddVacancy(me, "We need C#");
        if (hasResume)
        {
            resumes.Add(me);
        }

        if (hasRequirements)
        {
            requirements.Add(me, "C#", RequirementKind.MustHave);
        }

        var result = await handler.HandleAsync(new OpenVacancyQuery(me, vacancyId), CancellationToken.None);

        var opened = Assert.IsType<OpenVacancyResult.Opened>(result);
        var nothing = Assert.IsType<VacancyMatchOutcome.NothingToScore>(opened.Match);
        Assert.Equal(expectedMissing, nothing.Missing.ToArray());
        Assert.Empty(engine.Calls);
    }

    [Fact]
    public async Task Another_users_resume_does_not_count()
    {
        var vacancyId = AddVacancy(me, "We need C#");
        requirements.Add(me, "C#", RequirementKind.MustHave);
        resumes.Add(someoneElse);

        var result = await handler.HandleAsync(new OpenVacancyQuery(me, vacancyId), CancellationToken.None);

        var opened = Assert.IsType<OpenVacancyResult.Opened>(result);
        var nothing = Assert.IsType<VacancyMatchOutcome.NothingToScore>(opened.Match);
        Assert.Equal([MatchInput.Resume], nothing.Missing.ToArray());
        Assert.Empty(engine.Calls);
    }

    [Fact]
    public async Task Only_my_own_requirements_are_matched()
    {
        var vacancyId = AddVacancy(me, "C# and Kotlin");
        resumes.Add(me);
        resumes.Add(someoneElse);
        requirements.Add(me, "C#", RequirementKind.MustHave);
        requirements.Add(someoneElse, "Kotlin", RequirementKind.MustHave);

        var result = await handler.HandleAsync(new OpenVacancyQuery(me, vacancyId), CancellationToken.None);

        var opened = Assert.IsType<OpenVacancyResult.Opened>(result);
        var scored = Assert.IsType<VacancyMatchOutcome.Scored>(opened.Match);
        Assert.Equal("C#", Assert.Single(scored.Result.Verdicts).Term);
        Assert.Equal("C#", Assert.Single(Assert.Single(engine.Calls).Requirements).Term);
    }

    [Fact]
    public async Task Scoring_again_after_the_requirements_change_uses_the_current_ones()
    {
        var vacancyId = AddVacancy(me, "We need C#");
        resumes.Add(me);
        requirements.Add(me, "Java", RequirementKind.MustHave);

        var first = await handler.HandleAsync(new OpenVacancyQuery(me, vacancyId), CancellationToken.None);
        requirements.Clear(me);
        requirements.Add(me, "C#", RequirementKind.MustHave);
        var second = await handler.HandleAsync(new OpenVacancyQuery(me, vacancyId), CancellationToken.None);

        var before = Assert.IsType<VacancyMatchOutcome.Scored>(Assert.IsType<OpenVacancyResult.Opened>(first).Match);
        var after = Assert.IsType<VacancyMatchOutcome.Scored>(Assert.IsType<OpenVacancyResult.Opened>(second).Match);
        Assert.Equal(Verdict.NotMet, Assert.Single(before.Result.Verdicts).Verdict);
        Assert.Equal("C#", Assert.Single(after.Result.Verdicts).Term);
        Assert.Equal(Verdict.Met, Assert.Single(after.Result.Verdicts).Verdict);
        Assert.True(after.Result.Score >= 50);
    }

    private Guid AddVacancy(Guid ownerId, string rawText)
    {
        var id = Guid.NewGuid();
        vacancies.Add(ownerId, new VacancyDetail(id, "Title", "Company", rawText, Now));

        return id;
    }

    private sealed class FakeVacancyQueries : IVacancyQueries
    {
        private readonly List<(Guid OwnerId, VacancyDetail Vacancy)> stored = [];

        public void Add(Guid ownerId, VacancyDetail vacancy) => stored.Add((ownerId, vacancy));

        public Task<IReadOnlyList<VacancyDetail>> ListAllAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VacancyDetail>>(stored.Where(s => s.OwnerId == ownerId).Select(s => s.Vacancy).ToList());

        public Task<VacancyDetail?> GetAsync(Guid ownerId, Guid vacancyId, CancellationToken cancellationToken) =>
            Task.FromResult(stored.Where(s => s.OwnerId == ownerId && s.Vacancy.Id == vacancyId).Select(s => s.Vacancy).FirstOrDefault());
    }

    private sealed class FakeResumeQueries : IResumeQueries
    {
        private readonly HashSet<Guid> owners = [];

        public void Add(Guid ownerId) => owners.Add(ownerId);

        public Task<ResumeReadModel?> GetAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult(owners.Contains(ownerId) ? new ResumeReadModel("Some resume text", Now) : null);
    }

    private sealed class FakeRequirementQueries : IRequirementQueries
    {
        private readonly List<(Guid OwnerId, RequirementReadModel Requirement)> stored = [];

        public void Add(Guid ownerId, string text, RequirementKind kind) =>
            stored.Add((ownerId, new RequirementReadModel(Guid.NewGuid(), text, kind, Now)));

        public void Clear(Guid ownerId) => stored.RemoveAll(s => s.OwnerId == ownerId);

        public Task<PagedResult<RequirementReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<RequirementReadModel>> ListAllAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RequirementReadModel>>(stored.Where(s => s.OwnerId == ownerId).Select(s => s.Requirement).ToList());
    }

    private sealed class RecordingMatchEngine : IMatchEngine
    {
        public List<(string VacancyText, IReadOnlyList<MatchRequirement> Requirements)> Calls { get; } = [];

        public Task<MatchResult> MatchAsync(string vacancyText, IReadOnlyList<MatchRequirement> requirements, CancellationToken cancellationToken)
        {
            Calls.Add((vacancyText, requirements));

            return Task.FromResult(VacancyScorer.Score(vacancyText, requirements));
        }
    }
}
