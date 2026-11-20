using System.Security.Claims;
using Sift.Api.Errors;
using Sift.Api.Identity;
using Sift.Application.Common;
using Sift.Application.Recruiters;

namespace Sift.Api.Recruiters;

public static class RecruiterEndpoints
{
    public static IEndpointRouteBuilder MapRecruiterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/recruiters").WithTags("Recruiters").RequireAuthorization();

        group.MapPost("/", async (
                CreateRecruiterRequest request,
                ClaimsPrincipal principal,
                CreateRecruiterHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.InvalidToken();
                }

                var result = await handler.HandleAsync(
                    new CreateRecruiterCommand(userId, request.Name, request.Email, request.Phone, request.LinkedInUrl),
                    cancellationToken);

                return result switch
                {
                    CreateRecruiterResult.Created created => Results.Json(
                        new RecruiterResponse(
                            created.Recruiter.Id,
                            created.Recruiter.Name,
                            created.Recruiter.Email,
                            created.Recruiter.Phone,
                            created.Recruiter.LinkedInUrl,
                            created.Recruiter.CreatedAt),
                        statusCode: StatusCodes.Status201Created),
                    CreateRecruiterResult.Invalid invalid => ProblemResults.Validation(invalid.Errors),
                    _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
                };
            })
            .WithName("Recruiters_create");

        group.MapGet("/", async (
                string? page,
                string? pageSize,
                ClaimsPrincipal principal,
                IRecruiterQueries queries,
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

                var recruiters = await queries.ListAsync(userId, pageRequest, cancellationToken);

                return Results.Ok(new RecruiterPageResponse(
                    recruiters.Items
                        .Select(recruiter => new RecruiterResponse(
                            recruiter.Id,
                            recruiter.Name,
                            recruiter.Email,
                            recruiter.Phone,
                            recruiter.LinkedInUrl,
                            recruiter.CreatedAt))
                        .ToList(),
                    recruiters.Total,
                    pageRequest.Page,
                    pageRequest.PageSize));
            })
            .WithName("Recruiters_list");

        return endpoints;
    }
}
