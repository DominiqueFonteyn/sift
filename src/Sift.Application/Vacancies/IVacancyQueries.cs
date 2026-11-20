namespace Sift.Application.Vacancies;

public interface IVacancyQueries
{
    // Every vacancy the owner has, in no particular order: ranking and paging are the caller's job because scores are never stored; scoring every owned vacancy per request is an MVP trade-off to revisit with FR-7.5.
    Task<IReadOnlyList<VacancyDetail>> ListAllAsync(Guid ownerId, CancellationToken cancellationToken);

    // Null both when the vacancy does not exist and when it belongs to someone else.
    Task<VacancyDetail?> GetAsync(Guid ownerId, Guid vacancyId, CancellationToken cancellationToken);
}
