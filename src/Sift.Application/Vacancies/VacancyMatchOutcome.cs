using Sift.Application.Matching;
using Sift.Domain.Matching;

namespace Sift.Application.Vacancies;

public abstract record VacancyMatchOutcome
{
    private VacancyMatchOutcome()
    {
    }

    public sealed record Scored(MatchResult Result) : VacancyMatchOutcome;

    public sealed record NothingToScore(IReadOnlyList<MatchInput> Missing) : VacancyMatchOutcome;
}
