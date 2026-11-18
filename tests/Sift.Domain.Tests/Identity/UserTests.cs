using Sift.Domain.Identity;

namespace Sift.Domain.Tests.Identity;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 14, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void Registers_a_user_with_the_given_details_in_utc()
    {
        var id = Guid.NewGuid();

        var user = User.Register(id, Email.Create("me@example.com"), "hash", Now);

        Assert.Equal(id, user.Id);
        Assert.Equal("me@example.com", user.Email.Value);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal(Now, user.CreatedAt);
        Assert.Equal(TimeSpan.Zero, user.CreatedAt.Offset);
    }

    [Fact]
    public void Requires_an_identifier()
    {
        Assert.Throws<ArgumentException>(() => User.Register(Guid.Empty, Email.Create("me@example.com"), "hash", Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Requires_a_password_hash(string hash)
    {
        Assert.Throws<ArgumentException>(() => User.Register(Guid.NewGuid(), Email.Create("me@example.com"), hash, Now));
    }
}
