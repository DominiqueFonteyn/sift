namespace Sift.Domain.Requirements;

public sealed class Requirement
{
    public const int TextMaxLength = 200;

    // For persistence only.
    private Requirement()
    {
        Text = null!;
        NormalizedText = null!;
    }

    private Requirement(Guid id, Guid ownerId, string text, RequirementKind kind, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerId = ownerId;
        Text = text;
        NormalizedText = Normalize(text);
        Kind = kind;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Text { get; private set; }

    // Trimmed and case-folded; the per-user uniqueness key.
    public string NormalizedText { get; private set; }

    public RequirementKind Kind { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static string Normalize(string text) => text.Trim().ToLowerInvariant();

    public static bool TryValidateText(string? text, out string? error)
    {
        error = string.IsNullOrWhiteSpace(text)
            ? "A requirement needs text."
            : text.Length > TextMaxLength
                ? $"A requirement can be at most {TextMaxLength} characters."
                : null;

        return error is null;
    }

    public static Requirement Create(Guid id, Guid ownerId, string text, RequirementKind kind, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A requirement needs an identifier.", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A requirement needs an owner.", nameof(ownerId));
        }

        if (!TryValidateText(text, out var error))
        {
            throw new ArgumentException(error, nameof(text));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "A requirement is either a must-have or a nice-to-have.");
        }

        return new Requirement(id, ownerId, text.Trim(), kind, createdAt.ToUniversalTime());
    }
}
