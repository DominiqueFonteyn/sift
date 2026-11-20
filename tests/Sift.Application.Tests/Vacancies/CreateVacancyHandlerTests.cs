using Sift.Application.Common;
using Sift.Application.Vacancies;
using Sift.Domain.Vacancies;

namespace Sift.Application.Tests.Vacancies;

public sealed class CreateVacancyHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly FakeVacancyRepository vacancies = new();
    private readonly CreateVacancyHandler handler;

    public CreateVacancyHandlerTests() => handler = new CreateVacancyHandler(vacancies, new FixedClock());

    [Fact]
    public async Task Creates_the_vacancy_for_the_owner_in_the_command_using_the_clock()
    {
        var ownerId = Guid.NewGuid();

        var result = await handler.HandleAsync(new CreateVacancyCommand(ownerId, " Backend Engineer ", "Acme", "We need C# and SQL"), CancellationToken.None);

        var created = Assert.IsType<CreateVacancyResult.Created>(result);
        Assert.Equal(ownerId, created.Vacancy.OwnerId);
        Assert.Equal("Backend Engineer", created.Vacancy.Title);
        Assert.Equal(Now, created.Vacancy.CreatedAt);
        Assert.Same(created.Vacancy, Assert.Single(vacancies.Added));
    }

    [Theory]
    [InlineData(null, "Acme", "text", "title")]
    [InlineData(" ", "Acme", "text", "title")]
    [InlineData("Title", null, "text", "company")]
    [InlineData("Title", "\t\n ", "text", "company")]
    [InlineData("Title", "Acme", null, "rawText")]
    [InlineData("Title", "Acme", "   ", "rawText")]
    public async Task Rejects_an_invalid_field_keyed_by_that_field_and_stores_nothing(string? title, string? company, string? rawText, string field)
    {
        var result = await handler.HandleAsync(new CreateVacancyCommand(Guid.NewGuid(), title, company, rawText), CancellationToken.None);

        var invalid = Assert.IsType<CreateVacancyResult.Invalid>(result);
        Assert.Equal(field, Assert.Single(invalid.Errors).Key);
        Assert.Empty(vacancies.Added);
    }

    [Fact]
    public async Task Reports_every_invalid_field_at_once()
    {
        var result = await handler.HandleAsync(new CreateVacancyCommand(Guid.NewGuid(), "", "", ""), CancellationToken.None);

        var invalid = Assert.IsType<CreateVacancyResult.Invalid>(result);
        Assert.Equal(new[] { "company", "rawText", "title" }, invalid.Errors.Keys.Order().ToArray());
    }

    private sealed class FakeVacancyRepository : IVacancyRepository
    {
        public List<Vacancy> Added { get; } = [];

        public Task AddAsync(Vacancy vacancy, CancellationToken cancellationToken)
        {
            Added.Add(vacancy);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
