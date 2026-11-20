namespace Sift.Application.Requirements;

public sealed record DeleteRequirementCommand(Guid OwnerId, Guid RequirementId);
