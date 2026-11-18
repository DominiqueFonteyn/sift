namespace Sift.Domain.Identity;

public static class PasswordPolicy
{
    public const int MinLength = 8;

    public const int MaxLength = 128;

    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return "Password is required.";
        }

        if (password.Length < MinLength)
        {
            return $"Password must be at least {MinLength} characters long.";
        }

        if (password.Length > MaxLength)
        {
            return $"Password must be at most {MaxLength} characters long.";
        }

        return null;
    }
}
