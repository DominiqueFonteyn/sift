using System.Security.Claims;
using Sift.Api.Errors;
using Sift.Api.Identity;
using Sift.Application.Common;
using Sift.Application.Vacancies;

namespace Sift.Api.Vacancies;

public static class VacancyEndpoints
{
    public static IEndpointRouteBuilder MapVacancyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/vacancies").WithTags("Vacancies").RequireAuthorization();

        group.MapPost("/", async (
                CreateVacancyRequest request,
                ClaimsPrincipal principal,
                CreateVacancyHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.InvalidToken();
                }

                var result = await handler.HandleAsync(
                    new CreateVacancyCommand(userId, request.Title, request.Company, request.RawText),
                    cancellationToken);

                return result switch
                {
                    CreateVacancyResult.Created created => Results.Json(
                        new VacancyResponse(
                            created.Vacancy.Id,
                            created.Vacancy.Title,
                            created.Vacancy.Company,
                            created.Vacancy.RawText,
                            created.Vacancy.CreatedAt),
                        statusCode: StatusCodes.Status201Created),
                    CreateVacancyResult.Invalid invalid => ProblemResults.Validation(invalid.Errors),
                    _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
                };
            })
            .WithName("Vacancies_create");

        group.MapGet("/", async (
                string? page,
                string? pageSize,
                ClaimsPrincipal principal,
                IVacancyQueries queries,
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

                var vacancies = await queries.ListAsync(userId, pageRequest, cancellationToken);

                return Results.Ok(new VacancyPageResponse(
                    vacancies.Items.Select(vacancy => new VacancyListItemResponse(vacancy.Id, vacancy.Title, vacancy.Company, vacancy.CreatedAt)).ToList(),
                    vacancies.Total,
                    pageRequest.Page,
                    pageRequest.PageSize));
            })
            .WithName("Vacancies_list");

        // No route constraint: a malformed id must get the same problem body as a missing or foreign one.
        group.MapGet("/{id}", async (
                string id,
                ClaimsPrincipal principal,
                IVacancyQueries queries,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.InvalidToken();
                }

                if (!Guid.TryParse(id, out var vacancyId))
                {
                    return ProblemResults.NotFound();
                }

                var vacancy = await queries.GetAsync(userId, vacancyId, cancellationToken);

                return vacancy is null
                    ? ProblemResults.NotFound()
                    : Results.Ok(new VacancyResponse(vacancy.Id, vacancy.Title, vacancy.Company, vacancy.RawText, vacancy.CreatedAt));
            })
            .WithName("Vacancies_get");

        return endpoints;
    }
}
