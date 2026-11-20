namespace Sift.Api.Requirements;

public sealed record RequirementResponse(Guid Id, string Text, string Kind, DateTimeOffset CreatedAt);
