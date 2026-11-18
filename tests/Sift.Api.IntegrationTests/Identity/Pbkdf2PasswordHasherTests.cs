using Sift.Infrastructure.Identity;

namespace Sift.Api.IntegrationTests.Identity;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher hasher = new();

    [Fact]
    public void Verifies_the_original_password_and_rejects_others()
    {
        var hash = hasher.Hash("correct horse battery");

        Assert.True(hasher.Verify("correct horse battery", hash));
        Assert.False(hasher.Verify("Correct horse battery", hash));
    }

    [Fact]
    public void Salts_every_hash_and_never_embeds_the_password()
    {
        var first = hasher.Hash("correct horse battery");
        var second = hasher.Hash("correct horse battery");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("correct horse battery", first);
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("v1.abc.def.ghi")]
    [InlineData("v1.1000.!!!.???")]
    [InlineData("v2.1000.AAAA.AAAA")]
    public void Rejects_malformed_hashes_instead_of_throwing(string hash)
    {
        Assert.False(hasher.Verify("anything", hash));
    }
}
