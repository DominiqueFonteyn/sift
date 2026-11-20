using Sift.Application.Common;
using Sift.Application.Matching;

namespace Sift.Application.Vacancies;

public sealed record ListVacanciesResult(PagedResult<VacancyListItem> Page, IReadOnlyList<MatchInput> Missing);
