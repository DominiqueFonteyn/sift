using Sift.Application.Common;

namespace Sift.Application.Requirements;

public interface IRequirementQueries
{
    Task<PagedResult<RequirementReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken);
}
