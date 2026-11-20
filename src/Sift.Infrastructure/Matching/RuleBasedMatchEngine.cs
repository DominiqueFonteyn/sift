using Sift.Application.Matching;
using Sift.Domain.Matching;

namespace Sift.Infrastructure.Matching;

public sealed class RuleBasedMatchEngine : IMatchEngine
{
    public Task<MatchResult> MatchAsync(string vacancyText, IReadOnlyList<MatchRequirement> requirements, CancellationToken cancellationToken) =>
        Task.FromResult(VacancyScorer.Score(vacancyText, requirements));
}
