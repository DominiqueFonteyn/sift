using Sift.Application.Common;

namespace Sift.Application.Vacancies;

public interface IVacancyQueries
{
    // Newest first; ties on creation time are broken by id, descending.
    Task<PagedResult<VacancyListItem>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken);
}
