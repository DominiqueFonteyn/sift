namespace Sift.Application.Vacancies;

// The owner comes from the authenticated identity, never from the request body.
public sealed record CreateVacancyCommand(Guid OwnerId, string? Title, string? Company, string? RawText);
