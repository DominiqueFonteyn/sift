using Sift.Application.Common;
using Sift.Application.Recruiters;
using Sift.Domain.Recruiters;

namespace Sift.Application.Tests.Recruiters;

public sealed class CreateRecruiterHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly FakeRecruiterRepository recruiters = new();
    private readonly CreateRecruiterHandler handler;

    public CreateRecruiterHandlerTests() => handler = new CreateRecruiterHandler(recruiters, new FixedClock());

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

    private sealed class FakeRecruiterRepository : IRecruiterRepository
    {
        public List<Recruiter> Added { get; } = [];

        public Task AddAsync(Recruiter recruiter, CancellationToken cancellationToken)
        {
            Added.Add(recruiter);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
