using Sift.Application.Common;

namespace Sift.Application.Bureaus;

public interface IBureauQueries
{
    Task<PagedResult<BureauReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken);

    // Null both when the bureau does not exist and when it belongs to someone else.
    Task<BureauReadModel?> GetAsync(Guid ownerId, Guid bureauId, CancellationToken cancellationToken);
}
