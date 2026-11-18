namespace Sift.Domain.Bureaus;

public sealed class Bureau
{
    public const int NameMaxLength = 200;

    // For persistence only.
    private Bureau()
    {
        Name = null!;
    }

    private Bureau(Guid id, Guid ownerId, string name, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; }

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

    public static Bureau Create(Guid id, Guid ownerId, string name, DateTimeOffset createdAt)
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

        return new Bureau(id, ownerId, name.Trim(), createdAt.ToUniversalTime());
    }
}
