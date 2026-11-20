using Microsoft.EntityFrameworkCore;
using Sift.Application.Vacancies;
using Sift.Domain.Vacancies;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Vacancies;

public sealed class EfVacancyRepository(SiftDbContext dbContext) : IVacancyRepository
{
    public async Task AddAsync(Vacancy vacancy, CancellationToken cancellationToken)
    {
        dbContext.Set<Vacancy>().Add(vacancy);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveAsync(Guid ownerId, Guid vacancyId, CancellationToken cancellationToken)
    {
        var vacancy = await dbContext.Set<Vacancy>()
            .SingleOrDefaultAsync(candidate => candidate.Id == vacancyId && candidate.OwnerId == ownerId, cancellationToken);
        if (vacancy is null)
        {
            return false;
        }

        dbContext.Set<Vacancy>().Remove(vacancy);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Deleted concurrently by another request: the end state is the same as "not found".
            return false;
        }

        return true;
    }
}
