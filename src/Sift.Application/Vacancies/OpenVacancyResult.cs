namespace Sift.Application.Vacancies;

public abstract record OpenVacancyResult
{
    private OpenVacancyResult()
    {
    }

    public sealed record Opened(VacancyDetail Vacancy, VacancyMatchOutcome Match) : OpenVacancyResult;

    // Covers both a missing vacancy and one owned by someone else.
    public sealed record NotFound : OpenVacancyResult;
}
