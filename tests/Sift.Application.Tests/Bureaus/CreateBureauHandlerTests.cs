using Sift.Application.Bureaus;
using Sift.Application.Common;
using Sift.Domain.Bureaus;

namespace Sift.Application.Tests.Bureaus;

public sealed class CreateBureauHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly FakeBureauRepository bureaus = new();
    private readonly CreateBureauHandler handler;

    public CreateBureauHandlerTests() => handler = new CreateBureauHandler(bureaus, new FixedClock());

    [Fact]
    public async Task Creates_the_bureau_for_the_owner_in_the_command()
    {
        var ownerId = Guid.NewGuid();

        var result = await handler.HandleAsync(new CreateBureauCommand(ownerId, "Acme"), CancellationToken.None);

        var created = Assert.IsType<CreateBureauResult.Created>(result);
        Assert.Equal(ownerId, created.Bureau.OwnerId);
        Assert.Equal(Now, created.Bureau.CreatedAt);
        Assert.Same(created.Bureau, Assert.Single(bureaus.Added));
    }

    [Fact]
    public async Task Rejects_a_blank_name_and_stores_nothing()
    {
        var result = await handler.HandleAsync(new CreateBureauCommand(Guid.NewGuid(), " "), CancellationToken.None);

        var invalid = Assert.IsType<CreateBureauResult.Invalid>(result);
        Assert.Contains(CreateBureauHandler.NameField, invalid.Errors.Keys);
        Assert.Empty(bureaus.Added);
    }

    private sealed class FakeBureauRepository : IBureauRepository
    {
        public List<Bureau> Added { get; } = [];

        public Task AddAsync(Bureau bureau, CancellationToken cancellationToken)
        {
            Added.Add(bureau);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
