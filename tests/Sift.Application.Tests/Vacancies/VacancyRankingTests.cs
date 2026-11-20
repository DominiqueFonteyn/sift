using Sift.Application.Vacancies;
using Sift.Domain.Matching;

namespace Sift.Application.Tests.Vacancies;

public sealed class VacancyRankingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private const string LowId = "00000000-0000-0000-0000-000000000001";
    private const string MidId = "7a000000-0000-0000-0000-000000000001";
    private const string HighId = "ffffffff-0000-0000-0000-000000000001";

    public static TheoryData<string, (string Name, int? Score, int Minutes, string Id)[], string[]> Cases =>
        new()
        {
            { "higher score first", [("a", 40, 0, LowId), ("b", 90, 0, MidId), ("c", 60, 0, HighId)], ["b", "c", "a"] },
            { "score beats recency", [("old-high", 90, 0, LowId), ("new-low", 10, 5, MidId)], ["old-high", "new-low"] },
            { "equal score newest first", [("older", 70, 0, LowId), ("newer", 70, 5, MidId)], ["newer", "older"] },
            { "equal score and time id ascending", [("high-id", 70, 0, HighId), ("low-id", 70, 0, LowId), ("mid-id", 70, 0, MidId)], ["low-id", "mid-id", "high-id"] },
            { "unscored newest first", [("a", null, 0, LowId), ("b", null, 2, MidId), ("c", null, 1, HighId)], ["b", "c", "a"] },
            { "unscored same time id ascending", [("high-id", null, 0, HighId), ("low-id", null, 0, LowId)], ["low-id", "high-id"] },
            { "score zero still ranks by time", [("a", 0, 0, LowId), ("b", 0, 1, MidId)], ["b", "a"] }
        };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Ranks_by_score_then_newest_then_id(string scenario, (string Name, int? Score, int Minutes, string Id)[] rows, string[] expected)
    {
        Assert.NotEmpty(scenario);
        var items = rows.Select(row => new VacancyListItem(
            Guid.Parse(row.Id), row.Name, "Co", T0.AddMinutes(row.Minutes),
            row.Score is { } score ? new MatchResult(score, []) : null));

        var ranked = VacancyRanking.Rank(items);

        Assert.Equal(expected, ranked.Select(item => item.Title).ToArray());
    }
}
