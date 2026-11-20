namespace Sift.Domain.Matching;

public sealed record MatchResult(int Score, IReadOnlyList<RequirementVerdict> Verdicts);
