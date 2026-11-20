using Sift.Domain.Requirements;

namespace Sift.Domain.Matching;

public sealed record MatchRequirement(string Term, RequirementKind Kind);
