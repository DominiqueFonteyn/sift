namespace Sift.Api.Vacancies;

public sealed record VacancyResponse(Guid Id, string Title, string Company, string RawText, DateTimeOffset CreatedAt);
