namespace Sift.Api.Identity;

public sealed record RegisteredUserResponse(Guid Id, string Email, DateTimeOffset CreatedAt);
