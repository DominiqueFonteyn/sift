namespace Sift.Application.Identity;

public sealed record LoginUserCommand(string? Email, string? Password);
