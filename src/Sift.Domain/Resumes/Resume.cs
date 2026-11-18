using System.Globalization;

namespace Sift.Domain.Resumes;

public sealed class Resume
{
    // Counted in UTF-16 code units (string.Length), the same unit as JavaScript's .length on the client.
    public const int TextMaxLength = 20_000;

    // For persistence only.
    private Resume()
    {
        Text = null!;
    }

    private Resume(Guid ownerId, string text, DateTimeOffset updatedAt)
    {
        OwnerId = ownerId;
        Text = text;
        UpdatedAt = updatedAt;
    }

    public Guid OwnerId { get; private set; }

    public string Text { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static bool TryValidateText(string? text, out string? error)
    {
        error = string.IsNullOrWhiteSpace(text)
            ? "The resume cannot be empty."
            : text.Length > TextMaxLength
                ? $"The resume can be at most {TextMaxLength.ToString("N0", CultureInfo.InvariantCulture)} characters."
                : null;

        return error is null;
    }

    public static Resume Create(Guid ownerId, string text, DateTimeOffset now)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A resume needs an owner.", nameof(ownerId));
        }

        EnsureValid(text);

        return new Resume(ownerId, text, now.ToUniversalTime());
    }

    public void Replace(string text, DateTimeOffset now)
    {
        EnsureValid(text);

        Text = text;
        UpdatedAt = now.ToUniversalTime();
    }

    private static void EnsureValid(string text)
    {
        if (!TryValidateText(text, out var error))
        {
            throw new ArgumentException(error, nameof(text));
        }
    }
}
