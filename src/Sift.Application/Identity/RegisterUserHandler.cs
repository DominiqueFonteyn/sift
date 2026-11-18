using Sift.Application.Common;
using Sift.Domain.Identity;

namespace Sift.Application.Identity;

public sealed class RegisterUserHandler(IUserRepository users, IPasswordHasher passwordHasher, IClock clock)
{
    public const string EmailField = "email";

    public const string PasswordField = "password";

    public async Task<RegisterUserResult> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!Email.TryCreate(command.Email, out var email, out var emailError))
        {
            errors[EmailField] = [emailError!];
        }

        var passwordError = PasswordPolicy.Validate(command.Password);

        if (passwordError is not null)
        {
            errors[PasswordField] = [passwordError];
        }

        if (errors.Count > 0)
        {
            return new RegisterUserResult.Invalid(errors);
        }

        if (await users.EmailExistsAsync(email!, cancellationToken))
        {
            return new RegisterUserResult.EmailAlreadyInUse();
        }

        var user = User.Register(Guid.CreateVersion7(), email!, passwordHasher.Hash(command.Password!), clock.UtcNow);

        return await users.TryAddAsync(user, cancellationToken)
            ? new RegisterUserResult.Registered(user)
            : new RegisterUserResult.EmailAlreadyInUse();
    }
}
