namespace Sift.Application.Vacancies;

public sealed record VacancyListItem(Guid Id, string Title, string Company, DateTimeOffset CreatedAt);
