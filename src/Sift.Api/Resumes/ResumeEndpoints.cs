using System.Security.Claims;
using Sift.Api.Errors;
using Sift.Api.Identity;
using Sift.Application.Resumes;

namespace Sift.Api.Resumes;

public static class ResumeEndpoints
{
    public static IEndpointRouteBuilder MapResumeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/resume").WithTags("Resumes").RequireAuthorization();

        group.MapGet("/", async (
                ClaimsPrincipal principal,
                IResumeQueries queries,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.InvalidToken();
                }

                var resume = await queries.GetAsync(userId, cancellationToken);

                return resume is null
                    ? ProblemResults.NotFound()
                    : Results.Ok(new ResumeResponse(resume.Text, resume.UpdatedAt));
            })
            .WithName("Resumes_getMyResume");

        group.MapPut("/", async (
                SaveResumeRequest request,
                ClaimsPrincipal principal,
                SaveMyResumeHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!principal.TryGetUserId(out var userId))
                {
                    return ProblemResults.InvalidToken();
                }

                var result = await handler.HandleAsync(new SaveMyResumeCommand(userId, request.Text), cancellationToken);

                return result switch
                {
                    SaveMyResumeResult.Saved saved => Results.Ok(new ResumeResponse(saved.Resume.Text, saved.Resume.UpdatedAt)),
                    SaveMyResumeResult.Invalid invalid => ProblemResults.Validation(invalid.Errors),
                    _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
                };
            })
            .WithName("Resumes_saveMyResume");

        return endpoints;
    }
}
