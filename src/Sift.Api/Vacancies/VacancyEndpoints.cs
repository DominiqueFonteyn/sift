using System.Security.Claims;
using Sift.Api.Errors;
using Sift.Api.Identity;
using Sift.Application.Common;
using Sift.Application.Matching;
using Sift.Application.Vacancies;
using Sift.Domain.Matching;
using Sift.Domain.Requirements;

namespace Sift.Api.Vacancies;

public static class VacancyEndpoints
{
    private const string Met = "met";
    private const string NotMet = "not met";

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
                ListVacanciesHandler handler,
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

                var result = await handler.HandleAsync(new ListVacanciesQuery(userId, pageRequest), cancellationToken);

                return Results.Ok(new VacancyPageResponse(
                    result.Page.Items.Select(ToListItem).ToList(),
                    result.Page.Total,
                    pageRequest.Page,
                    pageRequest.PageSize,
                    result.Missing.Select(ToName).ToList()));
            })
            .WithName("Vacancies_list");

        // No route constraint: a malformed id must get the same problem body as a missing or foreign one.
        group.MapGet("/{id}", async (
                string id,
                ClaimsPrincipal principal,
                OpenVacancyHandler handler,
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

                var result = await handler.HandleAsync(new OpenVacancyQuery(userId, vacancyId), cancellationToken);

                return result switch
                {
                    OpenVacancyResult.Opened opened => Results.Ok(ToResponse(opened)),
                    OpenVacancyResult.NotFound => ProblemResults.NotFound(),
                    _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
                };
            })
            .WithName("Vacancies_get");

        return endpoints;
    }

    private static VacancyListItemResponse ToListItem(VacancyListItem item)
    {
        var verdicts = item.Match?.Verdicts.Select(ToResponse).ToList() ?? [];

        return new VacancyListItemResponse(
            item.Id, item.Title, item.Company, item.CreatedAt, item.Match?.Score, item.UnmetMustHaves.Count > 0, item.UnmetMustHaves, verdicts);
    }

    private static RequirementVerdictResponse ToResponse(RequirementVerdict verdict) => new(
        verdict.Term,
        RequirementKinds.ToName(verdict.Kind),
        verdict.Verdict == Verdict.Met ? Met : NotMet,
        verdict.Explanation);

    private static string ToName(MatchInput input) => input == MatchInput.Resume ? "resume" : "requirements";

    private static VacancyDetailResponse ToResponse(OpenVacancyResult.Opened opened) => new(
        opened.Vacancy.Id,
        opened.Vacancy.Title,
        opened.Vacancy.Company,
        opened.Vacancy.RawText,
        opened.Vacancy.CreatedAt,
        opened.Match switch
        {
            VacancyMatchOutcome.Scored scored => new VacancyMatchResponse(
                scored.Result.Score,
                scored.Result.Verdicts
                    .Select(verdict => new RequirementVerdictResponse(
                        verdict.Term,
                        RequirementKinds.ToName(verdict.Kind),
                        verdict.Verdict == Verdict.Met ? Met : NotMet,
                        verdict.Explanation))
                    .ToList(),
                []),
            VacancyMatchOutcome.NothingToScore nothing => new VacancyMatchResponse(
                null,
                [],
                nothing.Missing.Select(input => ToName(input)).ToList()),
            _ => throw new InvalidOperationException($"Unhandled outcome {opened.Match.GetType().Name}.")
        });
}
