using Sift.Domain.Resumes;

namespace Sift.Domain.Tests.Resumes;

public sealed class ResumeTests
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = T1.AddHours(2);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t\r\n ")]
    [InlineData(null)]
    public void Blank_text_is_rejected(string? text)
    {
        Assert.False(Resume.TryValidateText(text, out var error));
        Assert.Equal("The resume cannot be empty.", error);
        Assert.Throws<ArgumentException>(() => Resume.Create(Guid.NewGuid(), text!, T1));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(20_000, true)]
    [InlineData(20_001, false)]
    public void The_length_limit_is_counted_as_submitted(int length, bool valid)
    {
        var text = new string('a', length);

        Assert.Equal(valid, Resume.TryValidateText(text, out var error));
        Assert.Equal(valid, error is null);
        if (!valid)
        {
            Assert.Contains("20,000", error);
        }
    }

    [Fact]
    public void Surrounding_whitespace_counts_towards_the_limit_and_is_kept()
    {
        var text = "x" + new string(' ', 19_999);

        var resume = Resume.Create(Guid.NewGuid(), text, T1);

        Assert.Equal(text, resume.Text);
        Assert.False(Resume.TryValidateText(text + " ", out _));
    }

    [Fact]
    public void Length_is_measured_in_utf16_code_units()
    {
        var emoji = "\U0001F600";

        Assert.Equal(2, emoji.Length);
        Assert.True(Resume.TryValidateText(string.Concat(Enumerable.Repeat(emoji, 10_000)), out _));
        Assert.False(Resume.TryValidateText(string.Concat(Enumerable.Repeat(emoji, 10_000)) + "a", out _));
    }

    [Fact]
    public void Create_stamps_updated_at_from_the_given_time()
    {
        var ownerId = Guid.NewGuid();

        var resume = Resume.Create(ownerId, "My CV", T1);

        Assert.Equal(ownerId, resume.OwnerId);
        Assert.Equal(T1, resume.UpdatedAt);
    }

    [Fact]
    public void Replace_changes_text_and_moves_updated_at()
    {
        var resume = Resume.Create(Guid.NewGuid(), "old", T1);

        resume.Replace("new", T2);

        Assert.Equal("new", resume.Text);
        Assert.Equal(T2, resume.UpdatedAt);
    }

    [Fact]
    public void A_rejected_replace_leaves_the_resume_unchanged()
    {
        var resume = Resume.Create(Guid.NewGuid(), "old", T1);

        Assert.Throws<ArgumentException>(() => resume.Replace("  ", T2));
        Assert.Throws<ArgumentException>(() => resume.Replace(new string('a', 20_001), T2));

        Assert.Equal("old", resume.Text);
        Assert.Equal(T1, resume.UpdatedAt);
    }

    [Fact]
    public void An_empty_owner_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Resume.Create(Guid.Empty, "text", T1));
    }
}
