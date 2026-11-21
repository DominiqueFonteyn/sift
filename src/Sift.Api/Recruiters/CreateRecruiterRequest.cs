namespace Sift.Api.Recruiters;

// Unknown members such as an owner id are not bound, so they cannot spoof ownership.
// BureauId is a string so a malformed value becomes a validation problem instead of a binding failure.
public sealed record CreateRecruiterRequest(string? Name, string? Email, string? Phone, string? LinkedInUrl, string? BureauId);
