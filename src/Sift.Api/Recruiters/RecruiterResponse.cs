namespace Sift.Api.Recruiters;

public sealed record RecruiterResponse(Guid Id, string Name, string? Email, string? Phone, string? LinkedInUrl, DateTimeOffset CreatedAt);
