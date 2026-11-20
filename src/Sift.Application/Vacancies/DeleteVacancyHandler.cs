namespace Sift.Application.Vacancies;

public sealed class DeleteVacancyHandler(IVacancyRepository vacancies)
{
    public async Task<DeleteVacancyResult> HandleAsync(DeleteVacancyCommand command, CancellationToken cancellationToken)
    {
        var deleted = await vacancies.RemoveAsync(command.OwnerId, command.VacancyId, cancellationToken);

        return deleted ? new DeleteVacancyResult.Deleted() : new DeleteVacancyResult.NotFound();
    }
}
