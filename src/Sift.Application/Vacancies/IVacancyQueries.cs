using Sift.Application.Common;

namespace Sift.Application.Vacancies;

public interface IVacancyQueries
{
    // Newest first; ties on creation time are broken by id, descending.
    Task<PagedResult<VacancyListItem>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken);

    // Null both when the vacancy does not exist and when it belongs to someone else.
    Task<VacancyDetail?> GetAsync(Guid ownerId, Guid vacancyId, CancellationToken cancellationToken);
}
