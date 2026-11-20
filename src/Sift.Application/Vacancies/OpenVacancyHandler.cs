using Sift.Application.Matching;
using Sift.Application.Requirements;
using Sift.Application.Resumes;
using Sift.Domain.Matching;

namespace Sift.Application.Vacancies;

public sealed class OpenVacancyHandler(
    IVacancyQueries vacancies,
    IResumeQueries resumes,
    IRequirementQueries requirements,
    IMatchEngine matchEngine)
{
    public async Task<OpenVacancyResult> HandleAsync(OpenVacancyQuery query, CancellationToken cancellationToken)
    {
        // Ownership is established first: nothing else is read, and the matcher never runs, for a vacancy that is not the caller's.
        var vacancy = await vacancies.GetAsync(query.OwnerId, query.VacancyId, cancellationToken);
        if (vacancy is null)
        {
            return new OpenVacancyResult.NotFound();
        }

        var resume = await resumes.GetAsync(query.OwnerId, cancellationToken);
        var ownRequirements = await requirements.ListAllAsync(query.OwnerId, cancellationToken);

        var missing = new List<MatchInput>();
        if (resume is null)
        {
            missing.Add(MatchInput.Resume);
        }

        if (ownRequirements.Count == 0)
        {
            missing.Add(MatchInput.Requirements);
        }

        if (missing.Count > 0)
        {
            return new OpenVacancyResult.Opened(vacancy, new VacancyMatchOutcome.NothingToScore(missing));
        }

        var result = await matchEngine.MatchAsync(
            vacancy.RawText,
            ownRequirements.Select(requirement => new MatchRequirement(requirement.Text, requirement.Kind)).ToList(),
            cancellationToken);

        return new OpenVacancyResult.Opened(vacancy, new VacancyMatchOutcome.Scored(result));
    }
}
