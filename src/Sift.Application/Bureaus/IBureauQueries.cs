namespace Sift.Application.Bureaus;

public interface IBureauQueries
{
    Task<IReadOnlyList<BureauReadModel>> ListAsync(Guid ownerId, CancellationToken cancellationToken);

    // Null both when the bureau does not exist and when it belongs to someone else.
    Task<BureauReadModel?> GetAsync(Guid ownerId, Guid bureauId, CancellationToken cancellationToken);
}
