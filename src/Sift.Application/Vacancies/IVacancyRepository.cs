using Sift.Domain.Vacancies;

namespace Sift.Application.Vacancies;

public interface IVacancyRepository
{
    Task AddAsync(Vacancy vacancy, CancellationToken cancellationToken);

    // Only ever matches a vacancy owned by ownerId; returns false when there is nothing of theirs to remove.
    Task<bool> RemoveAsync(Guid ownerId, Guid vacancyId, CancellationToken cancellationToken);
}
