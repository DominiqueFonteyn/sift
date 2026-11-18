using Sift.Domain.Identity;

namespace Sift.Application.Identity;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken);

    // Returns false when the email is taken, so a concurrent registration cannot create a duplicate.
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken);
}
