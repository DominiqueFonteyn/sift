using Sift.Application.Vacancies;
using Sift.Domain.Vacancies;

namespace Sift.Application.Tests.Vacancies;

public sealed class DeleteVacancyHandlerTests
{
    private readonly FakeVacancyRepository vacancies = new();
    private readonly DeleteVacancyHandler handler;

    public DeleteVacancyHandlerTests() => handler = new DeleteVacancyHandler(vacancies);

    [Fact]
    public async Task Deletes_the_vacancy_for_the_owner_in_the_command()
    {
        var command = new DeleteVacancyCommand(Guid.NewGuid(), Guid.NewGuid());
        vacancies.Result = true;

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.IsType<DeleteVacancyResult.Deleted>(result);
        Assert.Equal((command.OwnerId, command.VacancyId), Assert.Single(vacancies.Removed));
    }

    [Fact]
    public async Task Reports_not_found_when_the_repository_removed_nothing()
    {
        vacancies.Result = false;

        var result = await handler.HandleAsync(new DeleteVacancyCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<DeleteVacancyResult.NotFound>(result);
    }

    private sealed class FakeVacancyRepository : IVacancyRepository
    {
        public bool Result { get; set; }

        public List<(Guid OwnerId, Guid VacancyId)> Removed { get; } = [];

        public Task AddAsync(Vacancy vacancy, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> RemoveAsync(Guid ownerId, Guid vacancyId, CancellationToken cancellationToken)
        {
            Removed.Add((ownerId, vacancyId));
            return Task.FromResult(Result);
        }
    }
}
