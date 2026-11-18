using Sift.Domain.Bureaus;

namespace Sift.Domain.Tests.Bureaus;

public sealed class BureauTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));

    [Fact]
    public void Creates_a_bureau_owned_by_the_given_user_with_audit_timestamps()
    {
        var ownerId = Guid.NewGuid();

        var bureau = Bureau.Create(Guid.NewGuid(), ownerId, "  Acme Staffing ", Now);

        Assert.Equal(ownerId, bureau.OwnerId);
        Assert.Equal("Acme Staffing", bureau.Name);
        Assert.Equal(Now.ToUniversalTime(), bureau.CreatedAt);
        Assert.Equal(bureau.CreatedAt, bureau.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_blank_name(string? name) =>
        Assert.False(Bureau.TryValidateName(name, out var error) || error is null);

    [Fact]
    public void Rejects_a_name_that_is_too_long() =>
        Assert.False(Bureau.TryValidateName(new string('a', Bureau.NameMaxLength + 1), out _));

    [Fact]
    public void Requires_an_owner() =>
        Assert.Throws<ArgumentException>(() => Bureau.Create(Guid.NewGuid(), Guid.Empty, "Acme", Now));

    [Fact]
    public void Requires_an_identifier() =>
        Assert.Throws<ArgumentException>(() => Bureau.Create(Guid.Empty, Guid.NewGuid(), "Acme", Now));
}
