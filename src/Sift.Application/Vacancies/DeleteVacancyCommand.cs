namespace Sift.Application.Vacancies;

public sealed record DeleteVacancyCommand(Guid OwnerId, Guid VacancyId);
