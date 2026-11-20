namespace Sift.Api.Requirements;

// Unknown members such as an owner id are not bound, so they cannot spoof ownership.
public sealed record CreateRequirementRequest(string? Text, string? Kind);
