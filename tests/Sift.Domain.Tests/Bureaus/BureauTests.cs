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
    public void Rejects_a_blank_name(string? name)
    {
        var valid = Bureau.TryValidateName(name, out var error);

        Assert.False(valid);
        Assert.Equal("A bureau needs a name.", error);
    }

    [Theory]
    [InlineData(Bureau.NameMaxLength, 0, true)]
    [InlineData(Bureau.NameMaxLength + 1, 0, false)]
    // The contract limits the raw string, so surrounding whitespace counts too.
    [InlineData(Bureau.NameMaxLength - 5, 10, false)]
    public void Limits_the_name_length_as_the_contract_does(int letters, int padding, bool expected)
    {
        var name = new string(' ', padding / 2) + new string('a', letters) + new string(' ', padding - padding / 2);

        Assert.Equal(expected, Bureau.TryValidateName(name, out _));
    }

    [Fact]
    public void Requires_an_owner() =>
        Assert.Throws<ArgumentException>(() => Bureau.Create(Guid.NewGuid(), Guid.Empty, "Acme", Now));

    [Fact]
    public void Requires_an_identifier() =>
        Assert.Throws<ArgumentException>(() => Bureau.Create(Guid.Empty, Guid.NewGuid(), "Acme", Now));
}
