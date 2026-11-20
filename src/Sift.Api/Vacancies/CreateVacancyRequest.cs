namespace Sift.Api.Vacancies;

// Unknown members such as an owner id, id or createdAt are not bound, so they cannot spoof ownership.
public sealed record CreateVacancyRequest(string? Title, string? Company, string? RawText);
