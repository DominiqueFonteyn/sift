using Sift.Application.Common;
using Sift.Application.Matching;
using Sift.Application.Requirements;
using Sift.Application.Resumes;
using Sift.Domain.Matching;

namespace Sift.Application.Vacancies;

public sealed class ListVacanciesHandler(
    IVacancyQueries vacancies,
    IResumeQueries resumes,
    IRequirementQueries requirements,
    IMatchEngine matchEngine)
{
    public async Task<ListVacanciesResult> HandleAsync(ListVacanciesQuery query, CancellationToken cancellationToken)
    {
        // Every read is scoped to the caller before anything is scored, ranked or paged.
        var owned = await vacancies.ListAllAsync(query.OwnerId, cancellationToken);
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

        var matchRequirements = ownRequirements.Select(requirement => new MatchRequirement(requirement.Text, requirement.Kind)).ToList();

        var items = new List<VacancyListItem>(owned.Count);
        foreach (var vacancy in owned)
        {
            MatchResult? match = null;
            if (missing.Count == 0)
            {
                match = await matchEngine.MatchAsync(vacancy.RawText, matchRequirements, cancellationToken);
            }

            items.Add(new VacancyListItem(vacancy.Id, vacancy.Title, vacancy.Company, vacancy.CreatedAt, match));
        }

        var ranked = VacancyRanking.Rank(items);
        var pageItems = ranked.Skip((int)Math.Min(query.Page.Offset, int.MaxValue)).Take(query.Page.PageSize).ToList();

        return new ListVacanciesResult(new PagedResult<VacancyListItem>(pageItems, ranked.Count), missing);
    }
}
