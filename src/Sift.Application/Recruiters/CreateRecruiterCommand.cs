namespace Sift.Application.Recruiters;

// The owner comes from the authenticated identity, never from the request body.
public sealed record CreateRecruiterCommand(Guid OwnerId, string? Name, string? Email, string? Phone, string? LinkedInUrl);
