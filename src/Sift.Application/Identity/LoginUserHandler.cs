using Sift.Domain.Identity;

namespace Sift.Application.Identity;

public sealed class LoginUserHandler(IUserRepository users, IPasswordHasher passwordHasher, ITokenIssuer tokenIssuer)
{
    public async Task<LoginUserResult> HandleAsync(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var password = command.Password ?? string.Empty;

        if (!Email.TryCreate(command.Email, out var email, out _))
        {
            // Spend the hashing time anyway so response time does not reveal which input was wrong.
            passwordHasher.Hash(password);
            return new LoginUserResult.InvalidCredentials();
        }

        var user = await users.FindByEmailAsync(email!, cancellationToken);

        if (user is null)
        {
            passwordHasher.Hash(password);
            return new LoginUserResult.InvalidCredentials();
        }

        return passwordHasher.Verify(password, user.PasswordHash)
            ? new LoginUserResult.LoggedIn(tokenIssuer.Issue(user.Id))
            : new LoginUserResult.InvalidCredentials();
    }
}
