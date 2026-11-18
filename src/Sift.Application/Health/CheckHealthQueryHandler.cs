using Sift.Domain.Health;

namespace Sift.Application.Health;

public sealed class CheckHealthQueryHandler(IDatabaseProbe databaseProbe)
{
    public async Task<HealthReport> HandleAsync(CancellationToken cancellationToken)
    {
        var reachable = await databaseProbe.CanReadAsync(cancellationToken);

        return reachable ? HealthReport.Healthy() : HealthReport.Unhealthy();
    }
}
