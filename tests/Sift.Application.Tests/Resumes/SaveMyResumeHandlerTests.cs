using Sift.Application.Common;
using Sift.Application.Resumes;
using Sift.Domain.Resumes;

namespace Sift.Application.Tests.Resumes;

public sealed class SaveMyResumeHandlerTests
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = T1.AddDays(1);

    private readonly FakeResumeRepository resumes = new();
    private readonly MutableClock clock = new() { UtcNow = T1 };
    private readonly SaveMyResumeHandler handler;

    public SaveMyResumeHandlerTests() => handler = new SaveMyResumeHandler(resumes, clock);

    [Fact]
    public async Task First_save_creates_the_resume_stamped_with_the_clock()
    {
        var ownerId = Guid.NewGuid();

        var result = await handler.HandleAsync(new SaveMyResumeCommand(ownerId, "CV"), CancellationToken.None);

        var saved = Assert.IsType<SaveMyResumeResult.Saved>(result);
        Assert.Equal(ownerId, saved.Resume.OwnerId);
        Assert.Equal(T1, saved.Resume.UpdatedAt);
        Assert.Single(resumes.Stored);
        Assert.Equal(1, resumes.Adds);
    }

    [Fact]
    public async Task Second_save_replaces_the_text_and_moves_updated_at_to_the_new_clock_time()
    {
        var ownerId = Guid.NewGuid();
        await handler.HandleAsync(new SaveMyResumeCommand(ownerId, "v1"), CancellationToken.None);
        clock.UtcNow = T2;

        var result = await handler.HandleAsync(new SaveMyResumeCommand(ownerId, "v2"), CancellationToken.None);

        var saved = Assert.IsType<SaveMyResumeResult.Saved>(result);
        Assert.Equal("v2", saved.Resume.Text);
        Assert.Equal(T2, saved.Resume.UpdatedAt);
        Assert.Single(resumes.Stored);
        Assert.Equal(1, resumes.Adds);
        Assert.Equal(1, resumes.Saves);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n ")]
    public async Task Blank_text_is_invalid_and_stores_nothing(string? text)
    {
        var result = await handler.HandleAsync(new SaveMyResumeCommand(Guid.NewGuid(), text), CancellationToken.None);

        var invalid = Assert.IsType<SaveMyResumeResult.Invalid>(result);
        var (field, messages) = Assert.Single(invalid.Errors);
        Assert.Equal(SaveMyResumeHandler.TextField, field);
        Assert.Equal("The resume cannot be empty.", Assert.Single(messages));
        Assert.Empty(resumes.Stored);
    }

    [Fact]
    public async Task Oversized_text_is_invalid_and_leaves_the_previous_resume()
    {
        var ownerId = Guid.NewGuid();
        await handler.HandleAsync(new SaveMyResumeCommand(ownerId, "keep"), CancellationToken.None);
        clock.UtcNow = T2;

        var result = await handler.HandleAsync(new SaveMyResumeCommand(ownerId, new string('a', 20_001)), CancellationToken.None);

        var invalid = Assert.IsType<SaveMyResumeResult.Invalid>(result);
        Assert.Contains("20,000", Assert.Single(Assert.Single(invalid.Errors).Value));
        var stored = Assert.Single(resumes.Stored).Value;
        Assert.Equal("keep", stored.Text);
        Assert.Equal(T1, stored.UpdatedAt);
    }

    [Fact]
    public async Task Exactly_20000_characters_is_saved()
    {
        var result = await handler.HandleAsync(new SaveMyResumeCommand(Guid.NewGuid(), new string('a', 20_000)), CancellationToken.None);

        Assert.IsType<SaveMyResumeResult.Saved>(result);
    }

    private sealed class FakeResumeRepository : IResumeRepository
    {
        public Dictionary<Guid, Resume> Stored { get; } = [];

        public int Adds { get; private set; }

        public int Saves { get; private set; }

        public Task<Resume?> FindByOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult(Stored.GetValueOrDefault(ownerId));

        public Task AddAsync(Resume resume, CancellationToken cancellationToken)
        {
            Adds++;
            Stored[resume.OwnerId] = resume;
            return Task.CompletedTask;
        }

        public Task SaveAsync(Resume resume, CancellationToken cancellationToken)
        {
            Saves++;
            return Task.CompletedTask;
        }
    }

    private sealed class MutableClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
