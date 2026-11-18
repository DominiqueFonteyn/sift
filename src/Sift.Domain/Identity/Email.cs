using System.Text.RegularExpressions;

namespace Sift.Domain.Identity;

public sealed partial record Email
{
    public const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static bool TryCreate(string? candidate, out Email? email, out string? error)
    {
        var normalised = candidate?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalised))
        {
            email = null;
            error = "Email is required.";
            return false;
        }

        if (normalised.Length > MaxLength || !Pattern().IsMatch(normalised))
        {
            email = null;
            error = "Email must be a valid email address.";
            return false;
        }

        email = new Email(normalised);
        error = null;
        return true;
    }

    public static Email Create(string? candidate) =>
        TryCreate(candidate, out var email, out var error)
            ? email!
            : throw new ArgumentException(error, nameof(candidate));

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
