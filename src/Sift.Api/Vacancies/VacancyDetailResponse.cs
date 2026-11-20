namespace Sift.Api.Vacancies;

public sealed record VacancyDetailResponse(
    Guid Id,
    string Title,
    string Company,
    string RawText,
    DateTimeOffset CreatedAt,
    VacancyMatchResponse Match);
