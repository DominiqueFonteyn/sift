using System.Security.Claims;
using Sift.Api.Errors;
using Sift.Api.Identity;
using Sift.Application.Bureaus;

namespace Sift.Api.Bureaus;

public static class BureauEndpoints
{
    public static IEndpointRouteBuilder MapBureauEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/bureaus").WithTags("Bureaus").RequireAuthorization();

        group.MapPost("/", async (
                CreateBureauRequest request,
                ClaimsPrincipal principal,
                CreateBureauHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.Unauthorized("The access token is invalid.");
                }

                var result = await handler.HandleAsync(new CreateBureauCommand(userId, request.Name), cancellationToken);

                return result switch
                {
                    CreateBureauResult.Created created => Results.Json(
                        new BureauResponse(created.Bureau.Id, created.Bureau.Name, created.Bureau.CreatedAt),
                        statusCode: StatusCodes.Status201Created),
                    CreateBureauResult.Invalid invalid => ProblemResults.Validation(invalid.Errors),
                    _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
                };
            })
            .WithName("Bureaus_create");

        group.MapGet("/", async (
                ClaimsPrincipal principal,
                IBureauQueries queries,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.Unauthorized("The access token is invalid.");
                }

                var bureaus = await queries.ListAsync(userId, cancellationToken);

                return Results.Ok(bureaus.Select(bureau => new BureauResponse(bureau.Id, bureau.Name, bureau.CreatedAt)));
            })
            .WithName("Bureaus_list");

        // No route constraint: a malformed id must get the same problem body as a missing or foreign one.
        group.MapGet("/{id}", async (
                string id,
                ClaimsPrincipal principal,
                IBureauQueries queries,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.Unauthorized("The access token is invalid.");
                }

                if (!Guid.TryParse(id, out var bureauId))
                {
                    return ProblemResults.NotFound();
                }

                var bureau = await queries.GetAsync(userId, bureauId, cancellationToken);

                return bureau is null
                    ? ProblemResults.NotFound()
                    : Results.Ok(new BureauResponse(bureau.Id, bureau.Name, bureau.CreatedAt));
            })
            .WithName("Bureaus_get");

        return endpoints;
    }
}
