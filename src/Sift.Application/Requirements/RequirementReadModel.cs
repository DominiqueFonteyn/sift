using Sift.Domain.Requirements;

namespace Sift.Application.Requirements;

public sealed record RequirementReadModel(Guid Id, string Text, RequirementKind Kind, DateTimeOffset CreatedAt);
