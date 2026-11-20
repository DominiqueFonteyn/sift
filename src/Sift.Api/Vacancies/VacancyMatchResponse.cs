namespace Sift.Api.Vacancies;

public sealed record VacancyMatchResponse(
    int? Score,
    IReadOnlyList<RequirementVerdictResponse> Verdicts,
    IReadOnlyList<string> MissingInputs);
