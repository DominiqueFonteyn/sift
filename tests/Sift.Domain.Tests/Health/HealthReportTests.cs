using Sift.Domain.Health;

namespace Sift.Domain.Tests.Health;

public sealed class HealthReportTests
{
    [Fact]
    public void Healthy_reports_a_healthy_status()
    {
        Assert.Equal(HealthStatus.Healthy, HealthReport.Healthy().Status);
    }

    [Fact]
    public void Unhealthy_reports_an_unhealthy_status()
    {
        Assert.Equal(HealthStatus.Unhealthy, HealthReport.Unhealthy().Status);
    }
}
