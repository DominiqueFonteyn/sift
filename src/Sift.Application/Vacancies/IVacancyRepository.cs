using Sift.Domain.Vacancies;

namespace Sift.Application.Vacancies;

public interface IVacancyRepository
{
    Task AddAsync(Vacancy vacancy, CancellationToken cancellationToken);
}
