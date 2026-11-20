using EmailAddress = Sift.Domain.Identity.Email;

namespace Sift.Domain.Recruiters;

public sealed class Recruiter
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = EmailAddress.MaxLength;
    public const int PhoneMaxLength = 50;
    public const int LinkedInUrlMaxLength = 2048;

    public const string NameField = "name";
    public const string EmailField = "email";
    public const string PhoneField = "phone";
    public const string LinkedInUrlField = "linkedInUrl";

    // For persistence only.
    private Recruiter()
    {
        Name = null!;
    }

    private Recruiter(Guid id, Guid ownerId, string name, string? email, string? phone, string? linkedInUrl, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        Email = email;
        Phone = phone;
        LinkedInUrl = linkedInUrl;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? LinkedInUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    // Lengths are counted as sent, like the contract does; an empty or whitespace-only optional field counts as not given.
    public static IReadOnlyDictionary<string, string[]> Validate(string? name, string? email, string? phone, string? linkedInUrl)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors[NameField] = ["A recruiter needs a name."];
        }
        else if (name.Length > NameMaxLength)
        {
            errors[NameField] = [$"A recruiter name can be at most {NameMaxLength} characters."];
        }

        if (!IsBlank(email) && (email!.Length > EmailMaxLength || !EmailAddress.TryCreate(email, out _, out _)))
        {
            errors[EmailField] = ["Email must be a valid email address."];
        }

        if (!IsBlank(phone) && phone!.Length > PhoneMaxLength)
        {
            errors[PhoneField] = [$"A phone number can be at most {PhoneMaxLength} characters."];
        }

        if (!IsBlank(linkedInUrl) && (linkedInUrl!.Length > LinkedInUrlMaxLength || !IsValidLinkedInUrl(linkedInUrl)))
        {
            errors[LinkedInUrlField] = ["LinkedIn URL must be an absolute http or https URL."];
        }

        return errors;
    }

    public static Recruiter Create(
        Guid id,
        Guid ownerId,
        string name,
        string? email,
        string? phone,
        string? linkedInUrl,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A recruiter needs an identifier.", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A recruiter needs an owner.", nameof(ownerId));
        }

        var errors = Validate(name, email, phone, linkedInUrl);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors.Values.SelectMany(messages => messages)), nameof(name));
        }

        return new Recruiter(id, ownerId, name.Trim(), Normalise(email), Normalise(phone), Normalise(linkedInUrl), createdAt.ToUniversalTime());
    }

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

    private static string? Normalise(string? value) => IsBlank(value) ? null : value!.Trim();

    private static bool IsValidLinkedInUrl(string value)
    {
        var candidate = value.Trim();

        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host)
            && !candidate.Any(char.IsWhiteSpace);
    }
}
