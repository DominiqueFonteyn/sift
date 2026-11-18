using Sift.Api.Errors;
using Sift.Application.Identity;

namespace Sift.Api.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/register", async (
                RegisterRequest request,
                RegisterUserHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new RegisterUserCommand(request.Email, request.Password), cancellationToken);

                return result switch
                {
                    RegisterUserResult.Registered registered => Results.Json(
                        new RegisteredUserResponse(registered.User.Id, registered.User.Email.Value, registered.User.CreatedAt),
                        statusCode: StatusCodes.Status201Created),
                    RegisterUserResult.Invalid invalid => ProblemResults.Validation(invalid.Errors),
                    RegisterUserResult.EmailAlreadyInUse => ProblemResults.Conflict("An account already exists for this email address."),
                    _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
                };
            })
            .WithName("Identity_register")
            .WithTags("Identity")
            .AllowAnonymous();

        return endpoints;
    }
}
