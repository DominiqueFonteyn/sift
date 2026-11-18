using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sift.Application.Identity;
using Sift.Domain.Identity;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Identity;

public sealed class EfUserRepository(SiftDbContext dbContext) : IUserRepository
{
    private const int SqliteConstraintErrorCode = 19;

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Set<User>().AnyAsync(user => user.Email == email, cancellationToken);

    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Set<User>().Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintErrorCode })
        {
            dbContext.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    public Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Set<User>().FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
}
