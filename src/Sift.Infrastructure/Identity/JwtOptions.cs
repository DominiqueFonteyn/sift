namespace Sift.Infrastructure.Identity;

public sealed record JwtOptions
{
    public const string SectionName = "Jwt";

    public const int MinimumSigningKeyLength = 32;

    public string SigningKey { get; init; } = string.Empty;

    public string Issuer { get; init; } = "sift";

    public string Audience { get; init; } = "sift";

    public TimeSpan Lifetime { get; init; } = TimeSpan.FromHours(1);

    public void EnsureValid()
    {
        if (SigningKey.Length < MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"Configuration value '{SectionName}:SigningKey' must be set to at least {MinimumSigningKeyLength} characters. " +
                "Use user-secrets or an environment variable; never commit it.");
        }

        if (Lifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Configuration value '{SectionName}:Lifetime' must be positive.");
        }
    }
}
