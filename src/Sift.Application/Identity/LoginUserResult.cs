namespace Sift.Application.Identity;

public abstract record LoginUserResult
{
    private LoginUserResult()
    {
    }

    public sealed record LoggedIn(IssuedToken Token) : LoginUserResult;

    // Deliberately one outcome for unknown email and wrong password, so callers cannot tell them apart.
    public sealed record InvalidCredentials : LoginUserResult;
}
