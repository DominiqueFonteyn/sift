namespace Sift.Domain.Health;

public sealed record HealthReport(HealthStatus Status)
{
    public static HealthReport Healthy() => new(HealthStatus.Healthy);

    public static HealthReport Unhealthy() => new(HealthStatus.Unhealthy);
}
