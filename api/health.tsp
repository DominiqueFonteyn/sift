import "@typespec/http";

using Http;

namespace Sift;

@doc("Reported liveness of the API.")
union HealthStatus {
  healthy: "healthy",
  unhealthy: "unhealthy",
}

@doc("The result of a health probe.")
model HealthResponse {
  @doc("Liveness of the API.")
  status: HealthStatus;
}

@route("/health")
@tag("Health")
interface Health {
  @doc("Report whether the API is healthy.")
  @get
  check(): HealthResponse | ProblemDetails;
}
