namespace Sift.Application.Recruiters;

public sealed record RecruiterReadModel(Guid Id, string Name, string? Email, string? Phone, string? LinkedInUrl, DateTimeOffset CreatedAt, RecruiterBureauReadModel? Bureau = null);
