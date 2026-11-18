using Sift.Domain.Identity;

namespace Sift.Application.Identity;

public abstract record RegisterUserResult
{
    private RegisterUserResult()
    {
    }

    public sealed record Registered(User User) : RegisterUserResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : RegisterUserResult;

    public sealed record EmailAlreadyInUse : RegisterUserResult;
}
