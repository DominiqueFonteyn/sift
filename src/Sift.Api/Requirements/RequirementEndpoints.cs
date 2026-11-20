using System.Security.Claims;
using Sift.Api.Errors;
using Sift.Api.Identity;
using Sift.Application.Common;
using Sift.Application.Requirements;
using Sift.Domain.Requirements;

namespace Sift.Api.Requirements;

public static class RequirementEndpoints
{
    public static IEndpointRouteBuilder MapRequirementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/requirements").WithTags("Requirements").RequireAuthorization();

        group.MapPost("/", async (
                CreateRequirementRequest request,
                ClaimsPrincipal principal,
                CreateRequirementHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.InvalidToken();
                }

                var result = await handler.HandleAsync(new CreateRequirementCommand(userId, request.Text, request.Kind), cancellationToken);

                return result switch
                {
                    CreateRequirementResult.Created created => Results.Json(
                        ToResponse(created.Requirement.Id, created.Requirement.Text, created.Requirement.Kind, created.Requirement.CreatedAt),
                        statusCode: StatusCodes.Status201Created),
                    CreateRequirementResult.Invalid invalid => ProblemResults.Validation(invalid.Errors),
                    CreateRequirementResult.Duplicate => ProblemResults.Conflict("You already have a requirement with this text."),
                    _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
                };
            })
            .WithName("Requirements_create");

        group.MapGet("/", async (
                string? page,
                string? pageSize,
                ClaimsPrincipal principal,
                IRequirementQueries queries,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.InvalidToken();
                }

                if (!PageRequest.TryCreate(page, pageSize, out var pageRequest, out var errors))
                {
                    return ProblemResults.Validation(errors);
                }

                var requirements = await queries.ListAsync(userId, pageRequest, cancellationToken);

                return Results.Ok(new RequirementPageResponse(
                    requirements.Items.Select(item => ToResponse(item.Id, item.Text, item.Kind, item.CreatedAt)).ToList(),
                    requirements.Total,
                    pageRequest.Page,
                    pageRequest.PageSize));
            })
            .WithName("Requirements_list");

        // No route constraint: a malformed id must get the same problem body as a missing or foreign one.
        group.MapDelete("/{id}", async (
                string id,
                ClaimsPrincipal principal,
                DeleteRequirementHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.InvalidToken();
                }

                if (!Guid.TryParse(id, out var requirementId))
                {
                    return ProblemResults.NotFound();
                }

                var result = await handler.HandleAsync(new DeleteRequirementCommand(userId, requirementId), cancellationToken);

                return result switch
                {
                    DeleteRequirementResult.Deleted => Results.NoContent(),
                    DeleteRequirementResult.NotFound => ProblemResults.NotFound(),
                    _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
                };
            })
            .WithName("Requirements_delete");

        return endpoints;
    }

    private static RequirementResponse ToResponse(Guid id, string text, RequirementKind kind, DateTimeOffset createdAt) =>
        new(id, text, RequirementKinds.ToName(kind), createdAt);
}
