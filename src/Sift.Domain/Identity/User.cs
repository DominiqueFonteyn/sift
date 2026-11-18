namespace Sift.Domain.Identity;

public sealed class User
{
    // For persistence only.
    private User()
    {
        Email = null!;
        PasswordHash = null!;
    }

    private User(Guid id, Email email, string passwordHash, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Email Email { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static User Register(Guid id, Email email, string passwordHash, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A user needs an identifier.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User(id, email, passwordHash, createdAt.ToUniversalTime());
    }
}
