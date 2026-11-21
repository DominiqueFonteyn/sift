using Sift.Domain.Recruiters;

namespace Sift.Domain.Tests.Recruiters;

public sealed class RecruiterTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));

    [Fact]
    public void Creates_a_recruiter_owned_by_the_given_user_with_a_trimmed_name()
    {
        var ownerId = Guid.NewGuid();

        var recruiter = Recruiter.Create(Guid.NewGuid(), ownerId, "  Jane Doe  ", " jane@acme.example ", "+32 470 12 34 56", "https://www.linkedin.com/in/jane", Now);

        Assert.Equal(ownerId, recruiter.OwnerId);
        Assert.Equal("Jane Doe", recruiter.Name);
        Assert.Equal("jane@acme.example", recruiter.Email);
        Assert.Equal("+32 470 12 34 56", recruiter.Phone);
        Assert.Equal("https://www.linkedin.com/in/jane", recruiter.LinkedInUrl);
        Assert.Equal(Now.ToUniversalTime(), recruiter.CreatedAt);
        Assert.Equal(recruiter.CreatedAt, recruiter.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Treats_empty_or_whitespace_optional_fields_as_not_given(string? blank)
    {
        Assert.Empty(Recruiter.Validate("Jane", blank, blank, blank));

        var recruiter = Recruiter.Create(Guid.NewGuid(), Guid.NewGuid(), "Jane", blank, blank, blank, Now);

        Assert.Null(recruiter.Email);
        Assert.Null(recruiter.Phone);
        Assert.Null(recruiter.LinkedInUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void Rejects_a_blank_name(string? name)
    {
        var errors = Recruiter.Validate(name, null, null, null);

        Assert.Equal(["A recruiter needs a name."], errors[Recruiter.NameField]);
    }

    [Theory]
    [InlineData(Recruiter.NameMaxLength, 0, true)]
    [InlineData(Recruiter.NameMaxLength + 1, 0, false)]
    // Length is counted as sent, so surrounding whitespace counts too.
    [InlineData(Recruiter.NameMaxLength - 5, 10, false)]
    public void Limits_the_name_length_as_the_contract_does(int letters, int padding, bool expected)
    {
        var name = new string(' ', padding / 2) + new string('a', letters) + new string(' ', padding - padding / 2);

        Assert.Equal(expected, !Recruiter.Validate(name, null, null, null).ContainsKey(Recruiter.NameField));
    }

    [Theory]
    [InlineData("jane@acme.example", true)]
    [InlineData("not-an-email", false)]
    [InlineData("jane@", false)]
    [InlineData("@acme.example", false)]
    [InlineData("jane doe@acme.example", false)]
    public void Checks_the_email_is_well_formed(string email, bool expected)
    {
        Assert.Equal(expected, !Recruiter.Validate("Jane", email, null, null).ContainsKey(Recruiter.EmailField));
    }

    [Fact]
    public void Limits_the_email_length()
    {
        var atLimit = new string('a', Recruiter.EmailMaxLength - "@acme.example".Length) + "@acme.example";

        Assert.DoesNotContain(Recruiter.EmailField, Recruiter.Validate("Jane", atLimit, null, null).Keys);
        Assert.Contains(Recruiter.EmailField, Recruiter.Validate("Jane", "a" + atLimit, null, null).Keys);
    }

    [Theory]
    [InlineData(Recruiter.PhoneMaxLength, true)]
    [InlineData(Recruiter.PhoneMaxLength + 1, false)]
    public void Limits_the_phone_length_but_not_its_format(int length, bool expected)
    {
        Assert.Equal(expected, !Recruiter.Validate("Jane", null, new string('x', length), null).ContainsKey(Recruiter.PhoneField));
    }

    [Theory]
    [InlineData("https://www.linkedin.com/in/johnsmith", true)]
    [InlineData("http://linkedin.com/in/x", true)]
    [InlineData("linkedin.com/in/x", false)]
    [InlineData("ftp://example.com/x", false)]
    [InlineData("https://", false)]
    [InlineData("https://linkedin.com/in/a b", false)]
    public void Requires_an_absolute_http_or_https_linkedin_url(string url, bool expected)
    {
        Assert.Equal(expected, !Recruiter.Validate("Jane", null, null, url).ContainsKey(Recruiter.LinkedInUrlField));
    }

    [Fact]
    public void Limits_the_linkedin_url_length()
    {
        var prefix = "https://linkedin.com/";
        var atLimit = prefix + new string('a', Recruiter.LinkedInUrlMaxLength - prefix.Length);

        Assert.DoesNotContain(Recruiter.LinkedInUrlField, Recruiter.Validate("Jane", null, null, atLimit).Keys);
        Assert.Contains(Recruiter.LinkedInUrlField, Recruiter.Validate("Jane", null, null, atLimit + "a").Keys);
    }

    [Fact]
    public void Has_no_bureau_unless_one_is_given()
    {
        Assert.Null(Recruiter.Create(Guid.NewGuid(), Guid.NewGuid(), "Jane", null, null, null, Now).BureauId);
    }

    [Fact]
    public void Links_to_the_given_bureau()
    {
        var bureauId = Guid.NewGuid();

        var recruiter = Recruiter.Create(Guid.NewGuid(), Guid.NewGuid(), "Jane", null, null, null, Now, bureauId);

        Assert.Equal(bureauId, recruiter.BureauId);
    }

    [Fact]
    public void Refuses_an_empty_bureau_id()
    {
        Assert.Throws<ArgumentException>(() => Recruiter.Create(Guid.NewGuid(), Guid.NewGuid(), "Jane", null, null, null, Now, Guid.Empty));
    }

    [Fact]
    public void Reports_every_invalid_field_at_once()
    {
        var errors = Recruiter.Validate("", "nope", new string('1', 51), "ftp://x");

        Assert.Equal(4, errors.Count);
    }

    [Fact]
    public void Refuses_to_create_an_invalid_recruiter()
    {
        Assert.Throws<ArgumentException>(() => Recruiter.Create(Guid.NewGuid(), Guid.NewGuid(), " ", null, null, null, Now));
        Assert.Throws<ArgumentException>(() => Recruiter.Create(Guid.Empty, Guid.NewGuid(), "Jane", null, null, null, Now));
        Assert.Throws<ArgumentException>(() => Recruiter.Create(Guid.NewGuid(), Guid.Empty, "Jane", null, null, null, Now));
    }
}
