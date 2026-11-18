namespace Sift.Application.Identity;

public interface ITokenIssuer
{
    IssuedToken Issue(Guid userId);
}
