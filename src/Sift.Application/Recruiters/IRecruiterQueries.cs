using Sift.Application.Common;

namespace Sift.Application.Recruiters;

public interface IRecruiterQueries
{
    Task<PagedResult<RecruiterReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken);
}
