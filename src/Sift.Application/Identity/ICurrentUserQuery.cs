namespace Sift.Application.Identity;

public interface ICurrentUserQuery
{
    Task<CurrentUserReadModel?> GetAsync(Guid userId, CancellationToken cancellationToken);
}
