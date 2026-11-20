namespace Sift.Application.Vacancies;

public abstract record DeleteVacancyResult
{
    private DeleteVacancyResult()
    {
    }

    public sealed record Deleted : DeleteVacancyResult;

    // Covers a missing, already-deleted and someone else's vacancy alike.
    public sealed record NotFound : DeleteVacancyResult;
}
