using Sift.Application.Bureaus;
using Sift.Application.Common;
using Sift.Application.Recruiters;
using Sift.Domain.Recruiters;

namespace Sift.Application.Tests.Recruiters;

public sealed class CreateRecruiterHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly FakeRecruiterRepository recruiters = new();
    private readonly FakeBureauQueries bureaus = new();
    private readonly CreateRecruiterHandler handler;

    public CreateRecruiterHandlerTests() => handler = new CreateRecruiterHandler(recruiters, bureaus, new FixedClock());

    [Fact]
    public async Task Creates_the_recruiter_for_the_owner_in_the_command()
    {
        var ownerId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new CreateRecruiterCommand(ownerId, "  Jane Doe  ", "jane@acme.example", null, null),
            CancellationToken.None);

        var created = Assert.IsType<CreateRecruiterResult.Created>(result);
        Assert.Equal(ownerId, created.Recruiter.OwnerId);
        Assert.Equal("Jane Doe", created.Recruiter.Name);
        Assert.Equal("jane@acme.example", created.Recruiter.Email);
        Assert.Equal(Now, created.Recruiter.CreatedAt);
        Assert.Null(created.Recruiter.BureauId);
        Assert.Null(created.Bureau);
        Assert.Same(created.Recruiter, Assert.Single(recruiters.Added));
    }

    [Fact]
    public async Task Treats_empty_optional_fields_as_not_given()
    {
        var result = await handler.HandleAsync(
            new CreateRecruiterCommand(Guid.NewGuid(), "Jane Doe", "", " ", ""),
            CancellationToken.None);

        var created = Assert.IsType<CreateRecruiterResult.Created>(result);
        Assert.Null(created.Recruiter.Email);
        Assert.Null(created.Recruiter.Phone);
        Assert.Null(created.Recruiter.LinkedInUrl);
    }

    [Fact]
    public async Task Rejects_invalid_input_with_an_error_per_field_and_stores_nothing()
    {
        var result = await handler.HandleAsync(
            new CreateRecruiterCommand(Guid.NewGuid(), " ", "not-an-email", new string('1', 51), "linkedin.com/in/x"),
            CancellationToken.None);

        var invalid = Assert.IsType<CreateRecruiterResult.Invalid>(result);
        Assert.Equal(
            [Recruiter.EmailField, Recruiter.LinkedInUrlField, Recruiter.NameField, Recruiter.PhoneField],
            invalid.Errors.Keys.Order());
        Assert.Empty(recruiters.Added);
    }

    [Fact]
    public async Task Links_the_recruiter_to_a_bureau_the_owner_has()
    {
        var ownerId = Guid.NewGuid();
        var bureau = bureaus.Add(ownerId, "Acme Recruitment");

        var result = await handler.HandleAsync(
            new CreateRecruiterCommand(ownerId, "Jane Doe", null, null, null, bureau.Id.ToString()),
            CancellationToken.None);

        var created = Assert.IsType<CreateRecruiterResult.Created>(result);
        Assert.Equal(bureau.Id, created.Recruiter.BureauId);
        Assert.Equal("Acme Recruitment", created.Bureau?.Name);
    }

    [Fact]
    public async Task Rejects_a_bureau_owned_by_someone_else_and_stores_nothing()
    {
        var bureau = bureaus.Add(Guid.NewGuid(), "Beta Talent");

        var result = await handler.HandleAsync(
            new CreateRecruiterCommand(Guid.NewGuid(), "Jane Doe", null, null, null, bureau.Id.ToString()),
            CancellationToken.None);

        Assert.IsType<CreateRecruiterResult.BureauNotFound>(result);
        Assert.Empty(recruiters.Added);
    }

    [Fact]
    public async Task Rejects_a_bureau_that_does_not_exist_and_stores_nothing()
    {
        var result = await handler.HandleAsync(
            new CreateRecruiterCommand(Guid.NewGuid(), "Jane Doe", null, null, null, Guid.NewGuid().ToString()),
            CancellationToken.None);

        Assert.IsType<CreateRecruiterResult.BureauNotFound>(result);
        Assert.Empty(recruiters.Added);
    }

    [Fact]
    public async Task Rejects_a_malformed_bureau_id_without_looking_the_bureau_up()
    {
        var result = await handler.HandleAsync(
            new CreateRecruiterCommand(Guid.NewGuid(), "Jane Doe", null, null, null, "not-a-uuid"),
            CancellationToken.None);

        var invalid = Assert.IsType<CreateRecruiterResult.Invalid>(result);
        Assert.Equal([Recruiter.BureauIdField], invalid.Errors.Keys);
        Assert.Equal(0, bureaus.Lookups);
        Assert.Empty(recruiters.Added);
    }

    [Fact]
    public async Task Validates_the_input_before_the_ownership_lookup()
    {
        var bureau = bureaus.Add(Guid.NewGuid(), "Beta Talent");

        var result = await handler.HandleAsync(
            new CreateRecruiterCommand(Guid.NewGuid(), " ", null, null, null, bureau.Id.ToString()),
            CancellationToken.None);

        var invalid = Assert.IsType<CreateRecruiterResult.Invalid>(result);
        Assert.Equal([Recruiter.NameField], invalid.Errors.Keys);
        Assert.Equal(0, bureaus.Lookups);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Treats_a_blank_bureau_id_as_no_bureau(string? bureauId)
    {
        var result = await handler.HandleAsync(
            new CreateRecruiterCommand(Guid.NewGuid(), "Jane Doe", null, null, null, bureauId),
            CancellationToken.None);

        var created = Assert.IsType<CreateRecruiterResult.Created>(result);
        Assert.Null(created.Recruiter.BureauId);
        Assert.Equal(0, bureaus.Lookups);
    }

    private sealed class FakeRecruiterRepository : IRecruiterRepository
    {
        public List<Recruiter> Added { get; } = [];

        public Task AddAsync(Recruiter recruiter, CancellationToken cancellationToken)
        {
            Added.Add(recruiter);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBureauQueries : IBureauQueries
    {
        private readonly List<(Guid OwnerId, BureauReadModel Bureau)> stored = [];

        public int Lookups { get; private set; }

        public BureauReadModel Add(Guid ownerId, string name)
        {
            var bureau = new BureauReadModel(Guid.NewGuid(), name, null, Now);
            stored.Add((ownerId, bureau));
            return bureau;
        }

        public Task<PagedResult<BureauReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BureauReadModel?> GetAsync(Guid ownerId, Guid bureauId, CancellationToken cancellationToken)
        {
            Lookups++;
            return Task.FromResult(stored.Where(entry => entry.OwnerId == ownerId && entry.Bureau.Id == bureauId).Select(entry => entry.Bureau).FirstOrDefault());
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
