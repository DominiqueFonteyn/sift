namespace Sift.Application.Requirements;

// The owner comes from the authenticated identity, never from the request body.
public sealed record CreateRequirementCommand(Guid OwnerId, string? Text, string? Kind);
