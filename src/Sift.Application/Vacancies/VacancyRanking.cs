namespace Sift.Application.Vacancies;

public static class VacancyRanking
{
    // Score highest first (unscored rows have none and so tie), then newest first, then lowercase canonical id ascending.
    public static IReadOnlyList<VacancyListItem> Rank(IEnumerable<VacancyListItem> items) =>
        items
            .OrderByDescending(item => item.Match?.Score ?? -1)
            .ThenByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Id.ToString("D").ToLowerInvariant(), StringComparer.Ordinal)
            .ToList();
}
