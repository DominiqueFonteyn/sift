import "@typespec/http";
import "@typespec/openapi";
import "./bureaus.tsp";
import "./health.tsp";
import "./identity.tsp";
import "./requirements.tsp";
import "./resumes.tsp";

using Http;
using OpenAPI;

@service(#{ title: "Sift API" })
@useAuth(BearerAuth)
@info(#{ version: "0.1.0" })
namespace Sift;

@doc("RFC 7807 problem details. Every error response uses this shape.")
@error
model ProblemDetails {
  @doc("A URI reference that identifies the problem type.")
  type?: string;

  @doc("A short, human-readable summary of the problem type.")
  title?: string;

  @doc("The HTTP status code generated for this occurrence of the problem.")
  status?: int32;

  @doc("A human-readable explanation specific to this occurrence of the problem.")
  detail?: string;

  @doc("A URI reference that identifies the specific occurrence of the problem.")
  instance?: string;
}
