using Sift.Application.Health;
using Sift.Domain.Health;

namespace Sift.Application.Tests.Health;

public sealed class CheckHealthQueryHandlerTests
{
    [Theory]
    [InlineData(true, HealthStatus.Healthy)]
    [InlineData(false, HealthStatus.Unhealthy)]
    public async Task Reports_the_status_of_the_database(bool reachable, HealthStatus expected)
    {
        var handler = new CheckHealthQueryHandler(new StubDatabaseProbe(reachable));

        var report = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(expected, report.Status);
    }

    private sealed class StubDatabaseProbe(bool reachable) : IDatabaseProbe
    {
        public Task<bool> CanReadAsync(CancellationToken cancellationToken) => Task.FromResult(reachable);
    }
}
