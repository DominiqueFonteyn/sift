using Sift.Domain.Requirements;

namespace Sift.Domain.Tests.Requirements;

public sealed class RequirementTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));

    [Fact]
    public void Creates_a_requirement_owned_by_the_given_user_with_trimmed_text()
    {
        var ownerId = Guid.NewGuid();

        var requirement = Requirement.Create(Guid.NewGuid(), ownerId, "  Remote-first ", RequirementKind.MustHave, Now);

        Assert.Equal(ownerId, requirement.OwnerId);
        Assert.Equal("Remote-first", requirement.Text);
        Assert.Equal(RequirementKind.MustHave, requirement.Kind);
        Assert.Equal(Now.ToUniversalTime(), requirement.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_text(string? text)
    {
        var valid = Requirement.TryValidateText(text, out var error);

        Assert.False(valid);
        Assert.Equal("A requirement needs text.", error);
    }

    [Theory]
    [InlineData(Requirement.TextMaxLength, true)]
    [InlineData(Requirement.TextMaxLength + 1, false)]
    public void Limits_the_text_length(int length, bool expected) =>
        Assert.Equal(expected, Requirement.TryValidateText(new string('a', length), out _));

    [Theory]
    [InlineData("Remote-first", "remote-first")]
    [InlineData("Remote-first", " Remote-first ")]
    [InlineData("KOTLIN", "kotlin")]
    public void Texts_differing_only_in_case_or_surrounding_whitespace_share_a_uniqueness_key(string first, string second)
    {
        var a = Requirement.Create(Guid.NewGuid(), Guid.NewGuid(), first, RequirementKind.MustHave, Now);
        var b = Requirement.Create(Guid.NewGuid(), Guid.NewGuid(), second, RequirementKind.NiceToHave, Now);

        Assert.Equal(a.NormalizedText, b.NormalizedText);
    }

    [Fact]
    public void Different_texts_have_different_uniqueness_keys()
    {
        var a = Requirement.Create(Guid.NewGuid(), Guid.NewGuid(), "Remote-first", RequirementKind.MustHave, Now);
        var b = Requirement.Create(Guid.NewGuid(), Guid.NewGuid(), "Kotlin", RequirementKind.MustHave, Now);

        Assert.NotEqual(a.NormalizedText, b.NormalizedText);
    }

    [Theory]
    [InlineData("must-have", RequirementKind.MustHave)]
    [InlineData("nice-to-have", RequirementKind.NiceToHave)]
    public void Parses_the_wire_names_of_the_two_kinds(string name, RequirementKind expected)
    {
        Assert.True(RequirementKinds.TryParse(name, out var kind));
        Assert.Equal(expected, kind);
        Assert.Equal(name, RequirementKinds.ToName(kind));
        Assert.Equal(expected, RequirementKinds.Parse(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("mandatory")]
    [InlineData("Must-Have")]
    public void Rejects_any_other_kind(string? name)
    {
        Assert.False(RequirementKinds.TryParse(name, out _));
        Assert.Throws<FormatException>(() => RequirementKinds.Parse(name!));
    }

    [Fact]
    public void Rejects_an_undefined_kind() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Requirement.Create(Guid.NewGuid(), Guid.NewGuid(), "Kotlin", (RequirementKind)99, Now));

    [Fact]
    public void Requires_an_owner() =>
        Assert.Throws<ArgumentException>(() => Requirement.Create(Guid.NewGuid(), Guid.Empty, "Kotlin", RequirementKind.MustHave, Now));

    [Fact]
    public void Requires_an_identifier() =>
        Assert.Throws<ArgumentException>(() => Requirement.Create(Guid.Empty, Guid.NewGuid(), "Kotlin", RequirementKind.MustHave, Now));
}
