namespace Sift.Api.Recruiters;

// Unknown members such as an owner id are not bound, so they cannot spoof ownership.
public sealed record CreateRecruiterRequest(string? Name, string? Email, string? Phone, string? LinkedInUrl);
