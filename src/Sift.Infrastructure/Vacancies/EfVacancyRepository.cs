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
}
