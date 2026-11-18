namespace Sift.Api.Identity;

public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);
