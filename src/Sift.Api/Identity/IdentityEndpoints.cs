using System.Security.Claims;
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

        endpoints.MapPost("/auth/login", async (
                LoginRequest request,
                LoginUserHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new LoginUserCommand(request.Email, request.Password), cancellationToken);

                return result switch
                {
                    LoginUserResult.LoggedIn loggedIn => Results.Ok(
                        new LoginResponse(loggedIn.Token.Value, "Bearer", loggedIn.Token.ExpiresAt)),
                    LoginUserResult.InvalidCredentials => ProblemResults.Unauthorized("The email or password is incorrect."),
                    _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
                };
            })
            .WithName("Identity_login")
            .WithTags("Identity")
            .AllowAnonymous();

        endpoints.MapGet("/auth/me", async (
                ClaimsPrincipal principal,
                ICurrentUserQuery query,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId))
                {
                    return ProblemResults.Unauthorized("The access token is invalid.");
                }

                var user = await query.GetAsync(userId, cancellationToken);

                return user is null
                    ? ProblemResults.Unauthorized("The access token is invalid.")
                    : Results.Ok(new CurrentUserResponse(user.Id, user.Email));
            })
            .WithName("Identity_me")
            .WithTags("Identity")
            .RequireAuthorization();

        return endpoints;
    }
}
