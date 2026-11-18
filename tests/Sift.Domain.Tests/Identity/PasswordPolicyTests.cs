using Sift.Domain.Identity;

namespace Sift.Domain.Tests.Identity;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData(PasswordPolicy.MinLength)]
    [InlineData(PasswordPolicy.MinLength + 1)]
    [InlineData(PasswordPolicy.MaxLength)]
    public void Accepts_passwords_within_the_length_bounds(int length)
    {
        Assert.Null(PasswordPolicy.Validate(new string('x', length)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Rejects_missing_or_too_short_passwords(string? password)
    {
        Assert.NotNull(PasswordPolicy.Validate(password));
    }

    [Fact]
    public void Rejects_passwords_longer_than_the_maximum()
    {
        Assert.NotNull(PasswordPolicy.Validate(new string('x', PasswordPolicy.MaxLength + 1)));
    }
}
