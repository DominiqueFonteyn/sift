namespace Sift.Application.Vacancies;

public sealed record VacancyDetail(Guid Id, string Title, string Company, string RawText, DateTimeOffset CreatedAt);
