using Sift.Domain.Matching;

namespace Sift.Application.Matching;

// The MVP implementation is a deterministic rule engine; the port lets another engine replace it later.
public interface IMatchEngine
{
    Task<MatchResult> MatchAsync(string vacancyText, IReadOnlyList<MatchRequirement> requirements, CancellationToken cancellationToken);
}
