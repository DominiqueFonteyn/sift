namespace Sift.Api.Vacancies;

public sealed record VacancyListItemResponse(
    Guid Id,
    string Title,
    string Company,
    DateTimeOffset CreatedAt,
    int? Score,
    bool FailsMustHave,
    IReadOnlyList<string> UnmetMustHaves,
    IReadOnlyList<RequirementVerdictResponse> Verdicts);
