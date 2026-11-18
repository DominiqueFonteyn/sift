using Sift.Application.Identity;
using Sift.Domain.Identity;

namespace Sift.Application.Tests.Identity;

public sealed class LoginUserHandlerTests
{
    private static readonly DateTimeOffset Expiry = new(2026, 11, 18, 13, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository users = new();
    private readonly RecordingTokenIssuer tokens = new();
    private readonly ReversingPasswordHasher hasher = new();
    private readonly LoginUserHandler handler;
    private readonly User user;

    public LoginUserHandlerTests()
    {
        user = User.Register(Guid.NewGuid(), Email.Create("me@example.com"), hasher.Hash("correct horse"), Expiry);
        users.Add(user);
        handler = new LoginUserHandler(users, hasher, tokens);
    }

    [Theory]
    [InlineData("me@example.com")]
    [InlineData("  ME@Example.com ")]
    public async Task Issues_a_token_for_the_user_when_the_credentials_are_correct(string email)
    {
        var result = await handler.HandleAsync(new LoginUserCommand(email, "correct horse"), CancellationToken.None);

        var loggedIn = Assert.IsType<LoginUserResult.LoggedIn>(result);
        Assert.Equal(user.Id, tokens.LastUserId);
        Assert.Equal(Expiry, loggedIn.Token.ExpiresAt);
    }

    [Theory]
    [InlineData("me@example.com", "wrong password")]
    [InlineData("me@example.com", "")]
    [InlineData("me@example.com", null)]
    [InlineData("nobody@example.com", "correct horse")]
    [InlineData("not-an-email", "correct horse")]
    [InlineData("", "correct horse")]
    [InlineData(null, null)]
    public async Task Gives_one_identical_failure_for_wrong_password_and_unknown_email(string? email, string? password)
    {
        var result = await handler.HandleAsync(new LoginUserCommand(email, password), CancellationToken.None);

        Assert.Equal(new LoginUserResult.InvalidCredentials(), result);
        Assert.Null(tokens.LastUserId);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> stored = [];

        public void Add(User user) => stored.Add(user);

        public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
            Task.FromResult(stored.Any(user => user.Email == email));

        public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
        {
            stored.Add(user);
            return Task.FromResult(true);
        }

        public Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken) =>
            Task.FromResult(stored.FirstOrDefault(user => user.Email == email));
    }

    private sealed class ReversingPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{new string(password.Reverse().ToArray())}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private sealed class RecordingTokenIssuer : ITokenIssuer
    {
        public Guid? LastUserId { get; private set; }

        public IssuedToken Issue(Guid userId)
        {
            LastUserId = userId;
            return new IssuedToken("token", Expiry);
        }
    }
}
