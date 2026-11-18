using Sift.Application.Health;
using Sift.Domain.Health;

namespace Sift.Api.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", async (CheckHealthQueryHandler handler, CancellationToken cancellationToken) =>
            {
                var report = await handler.HandleAsync(cancellationToken);

                return Results.Ok(new HealthResponse(Describe(report.Status)));
            })
            .WithName("Health_check")
            .WithTags("Health");

        return endpoints;
    }

    private static string Describe(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "healthy",
        _ => "unhealthy"
    };
}
