using Sift.Domain.Identity;

namespace Sift.Domain.Tests.Identity;

public sealed class EmailTests
{
    [Theory]
    [InlineData("me@example.com", "me@example.com")]
    [InlineData("  Me@Example.COM ", "me@example.com")]
    [InlineData("first.last+tag@sub.example.co.uk", "first.last+tag@sub.example.co.uk")]
    public void Accepts_and_normalises_valid_addresses(string candidate, string expected)
    {
        var created = Email.TryCreate(candidate, out var email, out var error);

        Assert.True(created);
        Assert.Null(error);
        Assert.Equal(expected, email?.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign.example.com")]
    [InlineData("two@@example.com")]
    [InlineData("missing-domain@")]
    [InlineData("@example.com")]
    [InlineData("no-tld@example")]
    [InlineData("white space@example.com")]
    [InlineData("dots@example..com")]
    public void Rejects_invalid_addresses(string? candidate)
    {
        var created = Email.TryCreate(candidate, out var email, out var error);

        Assert.False(created);
        Assert.Null(email);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Rejects_addresses_longer_than_the_maximum()
    {
        var candidate = new string('a', Email.MaxLength) + "@example.com";

        Assert.False(Email.TryCreate(candidate, out _, out _));
    }

    [Fact]
    public void Addresses_differing_only_by_case_are_equal()
    {
        Assert.Equal(Email.Create("ME@example.com"), Email.Create("me@EXAMPLE.com"));
    }
}
