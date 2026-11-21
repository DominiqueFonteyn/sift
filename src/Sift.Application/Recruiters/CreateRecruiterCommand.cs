namespace Sift.Application.Recruiters;

// The owner comes from the authenticated identity, never from the request body.
// BureauId stays a string so a malformed value can be reported as a validation error rather than a binding failure.
public sealed record CreateRecruiterCommand(Guid OwnerId, string? Name, string? Email, string? Phone, string? LinkedInUrl, string? BureauId = null);
