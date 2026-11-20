using Sift.Application.Common;

namespace Sift.Application.Vacancies;

public sealed record ListVacanciesQuery(Guid OwnerId, PageRequest Page);
