using Sift.Domain.Bureaus;

namespace Sift.Domain.Tests.Bureaus;

public sealed class BureauTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));

    [Fact]
    public void Creates_a_bureau_owned_by_the_given_user_with_audit_timestamps()
    {
        var ownerId = Guid.NewGuid();

        var bureau = Bureau.Create(Guid.NewGuid(), ownerId, "  Acme Staffing ", null, Now);

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
        Assert.Throws<ArgumentException>(() => Bureau.Create(Guid.NewGuid(), Guid.Empty, "Acme", null, Now));

    [Fact]
    public void Requires_an_identifier() =>
        Assert.Throws<ArgumentException>(() => Bureau.Create(Guid.Empty, Guid.NewGuid(), "Acme", null, Now));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t ")]
    public void A_blank_website_is_stored_as_none(string? website)
    {
        Assert.True(Bureau.TryValidateWebsite(website, out var error));
        Assert.Null(error);
        Assert.Null(Bureau.Create(Guid.NewGuid(), Guid.NewGuid(), "Acme", website, Now).Website);
    }

    [Fact]
    public void Trims_a_website_with_surrounding_whitespace()
    {
        var bureau = Bureau.Create(Guid.NewGuid(), Guid.NewGuid(), "Acme", "  https://acme.example  ", Now);

        Assert.Equal("https://acme.example", bureau.Website);
    }

    [Theory]
    [InlineData("https://acme.example", true)]
    [InlineData("http://acme.example/jobs?x=1", true)]
    [InlineData("acme.example", false)]
    [InlineData("/jobs", false)]
    [InlineData("ftp://acme.example", false)]
    [InlineData("mailto:a@acme.example", false)]
    [InlineData("https://acme example", false)]
    public void Accepts_only_absolute_http_or_https_websites(string website, bool expected)
    {
        Assert.Equal(expected, Bureau.TryValidateWebsite(website, out var error));
        Assert.Equal(expected, error is null);
    }

    [Theory]
    [InlineData(Bureau.WebsiteMaxLength, true)]
    [InlineData(Bureau.WebsiteMaxLength + 1, false)]
    public void Limits_the_website_length_as_the_contract_does(int length, bool expected)
    {
        const string prefix = "https://acme.example/";
        var website = prefix + new string('a', length - prefix.Length);

        Assert.Equal(length, website.Length);
        Assert.Equal(expected, Bureau.TryValidateWebsite(website, out _));
    }

    [Fact]
    public void Refuses_to_create_a_bureau_with_an_invalid_website() =>
        Assert.Throws<ArgumentException>(() => Bureau.Create(Guid.NewGuid(), Guid.NewGuid(), "Acme", "ftp://acme.example", Now));
}
