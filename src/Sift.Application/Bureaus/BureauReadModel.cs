namespace Sift.Application.Bureaus;

public sealed record BureauReadModel(Guid Id, string Name, string? Website, DateTimeOffset CreatedAt);
