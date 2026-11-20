namespace Sift.Api.Vacancies;

public sealed record VacancyPageResponse(IReadOnlyList<VacancyListItemResponse> Items, int Total, int Page, int PageSize);
