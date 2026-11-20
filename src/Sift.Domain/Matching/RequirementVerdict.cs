using Sift.Domain.Requirements;

namespace Sift.Domain.Matching;

public sealed record RequirementVerdict(string Term, RequirementKind Kind, Verdict Verdict, string Explanation);
