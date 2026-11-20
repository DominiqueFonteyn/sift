using Sift.Domain.Vacancies;

namespace Sift.Application.Vacancies;

public abstract record CreateVacancyResult
{
    private CreateVacancyResult()
    {
    }

    public sealed record Created(Vacancy Vacancy) : CreateVacancyResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : CreateVacancyResult;
}
