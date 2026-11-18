namespace Sift.Application.Identity;

public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);
