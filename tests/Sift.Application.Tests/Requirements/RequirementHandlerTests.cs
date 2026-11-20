using Sift.Application.Common;
using Sift.Application.Requirements;
using Sift.Domain.Requirements;

namespace Sift.Application.Tests.Requirements;

public sealed class RequirementHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly FakeRequirementRepository requirements = new();
    private readonly CreateRequirementHandler create;
    private readonly DeleteRequirementHandler delete;

    public RequirementHandlerTests()
    {
        create = new CreateRequirementHandler(requirements, new FixedClock());
        delete = new DeleteRequirementHandler(requirements);
    }

    [Fact]
    public async Task Creates_the_requirement_for_the_owner_in_the_command()
    {
        var ownerId = Guid.NewGuid();

        var result = await create.HandleAsync(new CreateRequirementCommand(ownerId, " Remote-first ", "must-have"), CancellationToken.None);

        var created = Assert.IsType<CreateRequirementResult.Created>(result);
        Assert.Equal(ownerId, created.Requirement.OwnerId);
        Assert.Equal("Remote-first", created.Requirement.Text);
        Assert.Equal(RequirementKind.MustHave, created.Requirement.Kind);
        Assert.Equal(Now, created.Requirement.CreatedAt);
        Assert.Same(created.Requirement, Assert.Single(requirements.Stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rejects_blank_text_and_stores_nothing(string? text)
    {
        var result = await create.HandleAsync(new CreateRequirementCommand(Guid.NewGuid(), text, "must-have"), CancellationToken.None);

        var invalid = Assert.IsType<CreateRequirementResult.Invalid>(result);
        Assert.Equal(CreateRequirementHandler.TextField, Assert.Single(invalid.Errors).Key);
        Assert.Empty(requirements.Stored);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("mandatory")]
    public async Task Rejects_a_missing_or_unknown_kind_and_stores_nothing(string? kind)
    {
        var result = await create.HandleAsync(new CreateRequirementCommand(Guid.NewGuid(), "Kotlin", kind), CancellationToken.None);

        var invalid = Assert.IsType<CreateRequirementResult.Invalid>(result);
        Assert.Equal(CreateRequirementHandler.KindField, Assert.Single(invalid.Errors).Key);
        Assert.Empty(requirements.Stored);
    }

    [Theory]
    [InlineData("Remote-first")]
    [InlineData("remote-first")]
    [InlineData("  REMOTE-FIRST  ")]
    public async Task Rejects_a_duplicate_of_the_same_users_text_ignoring_case_and_whitespace(string text)
    {
        var ownerId = Guid.NewGuid();
        await create.HandleAsync(new CreateRequirementCommand(ownerId, "Remote-first", "must-have"), CancellationToken.None);

        var result = await create.HandleAsync(new CreateRequirementCommand(ownerId, text, "nice-to-have"), CancellationToken.None);

        Assert.IsType<CreateRequirementResult.Duplicate>(result);
        Assert.Single(requirements.Stored);
    }

    [Fact]
    public async Task The_same_text_is_allowed_for_a_different_user()
    {
        await create.HandleAsync(new CreateRequirementCommand(Guid.NewGuid(), "Remote-first", "must-have"), CancellationToken.None);

        var result = await create.HandleAsync(new CreateRequirementCommand(Guid.NewGuid(), "Remote-first", "must-have"), CancellationToken.None);

        Assert.IsType<CreateRequirementResult.Created>(result);
        Assert.Equal(2, requirements.Stored.Count);
    }

    [Fact]
    public async Task Reports_a_duplicate_when_the_store_rejects_a_concurrent_insert()
    {
        requirements.RejectInserts = true;

        var result = await create.HandleAsync(new CreateRequirementCommand(Guid.NewGuid(), "Kotlin", "nice-to-have"), CancellationToken.None);

        Assert.IsType<CreateRequirementResult.Duplicate>(result);
    }

    [Fact]
    public async Task Deletes_the_owners_requirement()
    {
        var ownerId = Guid.NewGuid();
        var created = Assert.IsType<CreateRequirementResult.Created>(
            await create.HandleAsync(new CreateRequirementCommand(ownerId, "Kotlin", "nice-to-have"), CancellationToken.None));

        var result = await delete.HandleAsync(new DeleteRequirementCommand(ownerId, created.Requirement.Id), CancellationToken.None);

        Assert.IsType<DeleteRequirementResult.Deleted>(result);
        Assert.Empty(requirements.Stored);
    }

    [Fact]
    public async Task Does_not_delete_another_users_requirement()
    {
        var created = Assert.IsType<CreateRequirementResult.Created>(
            await create.HandleAsync(new CreateRequirementCommand(Guid.NewGuid(), "Kotlin", "nice-to-have"), CancellationToken.None));

        var result = await delete.HandleAsync(new DeleteRequirementCommand(Guid.NewGuid(), created.Requirement.Id), CancellationToken.None);

        Assert.IsType<DeleteRequirementResult.NotFound>(result);
        Assert.Single(requirements.Stored);
    }

    [Fact]
    public async Task Deleting_a_missing_requirement_is_not_found()
    {
        var result = await delete.HandleAsync(new DeleteRequirementCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<DeleteRequirementResult.NotFound>(result);
    }

    private sealed class FakeRequirementRepository : IRequirementRepository
    {
        public List<Requirement> Stored { get; } = [];

        public bool RejectInserts { get; set; }

        public Task<bool> TextExistsAsync(Guid ownerId, string normalizedText, CancellationToken cancellationToken) =>
            Task.FromResult(Stored.Any(item => item.OwnerId == ownerId && item.NormalizedText == normalizedText));

        public Task<bool> TryAddAsync(Requirement requirement, CancellationToken cancellationToken)
        {
            if (RejectInserts)
            {
                return Task.FromResult(false);
            }

            Stored.Add(requirement);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid ownerId, Guid requirementId, CancellationToken cancellationToken) =>
            Task.FromResult(Stored.RemoveAll(item => item.OwnerId == ownerId && item.Id == requirementId) > 0);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
