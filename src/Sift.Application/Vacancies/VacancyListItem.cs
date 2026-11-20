using Sift.Domain.Matching;
using Sift.Domain.Requirements;

namespace Sift.Application.Vacancies;

// Match is null when the vacancy could not be scored because a scoring input is missing.
public sealed record VacancyListItem(Guid Id, string Title, string Company, DateTimeOffset CreatedAt, MatchResult? Match)
{
    public IReadOnlyList<string> UnmetMustHaves { get; } = Match is null
        ? []
        : Match.Verdicts
            .Where(verdict => verdict.Kind == RequirementKind.MustHave && verdict.Verdict == Verdict.NotMet)
            .Select(verdict => verdict.Term)
            .ToList();
}
