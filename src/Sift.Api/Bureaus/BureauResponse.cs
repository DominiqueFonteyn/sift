namespace Sift.Api.Bureaus;

public sealed record BureauResponse(Guid Id, string Name, DateTimeOffset CreatedAt);
