namespace Sift.Domain.Bureaus;

public sealed class Bureau
{
    public const int NameMaxLength = 200;
    public const int WebsiteMaxLength = 2048;

    // For persistence only.
    private Bureau()
    {
        Name = null!;
    }

    private Bureau(Guid id, Guid ownerId, string name, string? website, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        Website = website;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; }

    public string? Website { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static bool TryValidateName(string? name, out string? error)
    {
        error = string.IsNullOrWhiteSpace(name)
            ? "A bureau needs a name."
            : name.Length > NameMaxLength
                ? $"A bureau name can be at most {NameMaxLength} characters."
                : null;

        return error is null;
    }

    // Length is counted as sent, like the contract does; an empty or whitespace-only website counts as not given.
    public static bool TryValidateWebsite(string? website, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(website))
        {
            return true;
        }

        var candidate = website.Trim();
        var valid = website.Length <= WebsiteMaxLength
            && Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host)
            && !candidate.Any(char.IsWhiteSpace);

        if (!valid)
        {
            error = "Website must be an absolute http or https URL.";
        }

        return valid;
    }

    public static Bureau Create(Guid id, Guid ownerId, string name, string? website, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A bureau needs an identifier.", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A bureau needs an owner.", nameof(ownerId));
        }

        if (!TryValidateName(name, out var error))
        {
            throw new ArgumentException(error, nameof(name));
        }

        if (!TryValidateWebsite(website, out var websiteError))
        {
            throw new ArgumentException(websiteError, nameof(website));
        }

        var normalisedWebsite = string.IsNullOrWhiteSpace(website) ? null : website.Trim();

        return new Bureau(id, ownerId, name.Trim(), normalisedWebsite, createdAt.ToUniversalTime());
    }
}
