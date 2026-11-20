using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sift.Application.Requirements;
using Sift.Domain.Requirements;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Requirements;

public sealed class EfRequirementRepository(SiftDbContext dbContext) : IRequirementRepository
{
    private const int SqliteConstraintErrorCode = 19;

    public Task<bool> TextExistsAsync(Guid ownerId, string normalizedText, CancellationToken cancellationToken) =>
        dbContext.Set<Requirement>().AnyAsync(
            requirement => requirement.OwnerId == ownerId && requirement.NormalizedText == normalizedText,
            cancellationToken);

    public async Task<bool> TryAddAsync(Requirement requirement, CancellationToken cancellationToken)
    {
        dbContext.Set<Requirement>().Add(requirement);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintErrorCode })
        {
            dbContext.Entry(requirement).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid requirementId, CancellationToken cancellationToken)
    {
        var requirement = await dbContext.Set<Requirement>()
            .FirstOrDefaultAsync(candidate => candidate.Id == requirementId && candidate.OwnerId == ownerId, cancellationToken);

        if (requirement is null)
        {
            return false;
        }

        dbContext.Set<Requirement>().Remove(requirement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
