using Sift.Application.Common;
using Sift.Application.Identity;
using Sift.Domain.Identity;

namespace Sift.Application.Tests.Identity;

public sealed class RegisterUserHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository users = new();
    private readonly RegisterUserHandler handler;

    public RegisterUserHandlerTests()
    {
        handler = new RegisterUserHandler(users, new ReversingPasswordHasher(), new FixedClock(Now));
    }

    [Fact]
    public async Task Registers_a_user_and_stores_only_the_hash()
    {
        var result = await handler.HandleAsync(new RegisterUserCommand(" Me@Example.com ", "correct horse"), CancellationToken.None);

        var registered = Assert.IsType<RegisterUserResult.Registered>(result);
        Assert.Equal("me@example.com", registered.User.Email.Value);
        Assert.Equal(Now, registered.User.CreatedAt);
        Assert.NotEqual("correct horse", registered.User.PasswordHash);
        Assert.Equal(new ReversingPasswordHasher().Hash("correct horse"), registered.User.PasswordHash);
        Assert.Single(users.Added);
    }

    [Fact]
    public async Task The_new_user_can_be_authenticated_with_the_original_credentials()
    {
        await handler.HandleAsync(new RegisterUserCommand("me@example.com", "correct horse"), CancellationToken.None);

        var stored = await users.FindByEmailAsync(Email.Create("me@example.com"), CancellationToken.None);

        Assert.NotNull(stored);
        Assert.True(new ReversingPasswordHasher().Verify("correct horse", stored.PasswordHash));
        Assert.False(new ReversingPasswordHasher().Verify("wrong password", stored.PasswordHash));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Rejects_a_weak_password_naming_the_password_field(string? password)
    {
        var result = await handler.HandleAsync(new RegisterUserCommand("me@example.com", password), CancellationToken.None);

        var invalid = Assert.IsType<RegisterUserResult.Invalid>(result);
        Assert.Contains(RegisterUserHandler.PasswordField, invalid.Errors.Keys);
        Assert.DoesNotContain(RegisterUserHandler.EmailField, invalid.Errors.Keys);
        Assert.Empty(users.Added);
    }

    [Fact]
    public async Task Rejects_an_invalid_email_naming_the_email_field()
    {
        var result = await handler.HandleAsync(new RegisterUserCommand("not-an-email", "correct horse"), CancellationToken.None);

        var invalid = Assert.IsType<RegisterUserResult.Invalid>(result);
        Assert.Contains(RegisterUserHandler.EmailField, invalid.Errors.Keys);
        Assert.Empty(users.Added);
    }

    [Fact]
    public async Task Reports_every_invalid_field_at_once()
    {
        var result = await handler.HandleAsync(new RegisterUserCommand("nope", "short"), CancellationToken.None);

        var invalid = Assert.IsType<RegisterUserResult.Invalid>(result);
        Assert.Equal(2, invalid.Errors.Count);
    }

    [Fact]
    public async Task Rejects_an_email_that_is_already_registered_regardless_of_case()
    {
        await handler.HandleAsync(new RegisterUserCommand("me@example.com", "correct horse"), CancellationToken.None);

        var result = await handler.HandleAsync(new RegisterUserCommand("ME@example.com", "another password"), CancellationToken.None);

        Assert.IsType<RegisterUserResult.EmailAlreadyInUse>(result);
        Assert.Single(users.Added);
    }

    [Fact]
    public async Task Reports_a_duplicate_when_the_repository_loses_a_concurrent_insert()
    {
        users.RejectInserts = true;

        var result = await handler.HandleAsync(new RegisterUserCommand("me@example.com", "correct horse"), CancellationToken.None);

        Assert.IsType<RegisterUserResult.EmailAlreadyInUse>(result);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Added { get; } = [];

        public bool RejectInserts { get; set; }

        public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
            Task.FromResult(Added.Any(user => user.Email == email));

        public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
        {
            if (RejectInserts || Added.Any(existing => existing.Email == user.Email))
            {
                return Task.FromResult(false);
            }

            Added.Add(user);
            return Task.FromResult(true);
        }

        public Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken) =>
            Task.FromResult(Added.FirstOrDefault(user => user.Email == email));
    }

    private sealed class ReversingPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{new string(password.Reverse().ToArray())}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
