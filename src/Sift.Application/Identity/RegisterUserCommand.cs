namespace Sift.Application.Identity;

public sealed record RegisterUserCommand(string? Email, string? Password);
