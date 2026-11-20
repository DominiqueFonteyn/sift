using Sift.Application.Common;

namespace Sift.Application.Requirements;

public interface IRequirementQueries
{
    Task<PagedResult<RequirementReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken);

    // Every requirement of the owner, oldest first; scoring needs the whole list, not a page.
    Task<IReadOnlyList<RequirementReadModel>> ListAllAsync(Guid ownerId, CancellationToken cancellationToken);
}
