using Sift.Domain.Requirements;

namespace Sift.Application.Requirements;

public interface IRequirementRepository
{
    Task<bool> TextExistsAsync(Guid ownerId, string normalizedText, CancellationToken cancellationToken);

    // False when the user already has the same text, including when a concurrent request won the race.
    Task<bool> TryAddAsync(Requirement requirement, CancellationToken cancellationToken);

    // False both when the requirement does not exist and when it belongs to someone else.
    Task<bool> DeleteAsync(Guid ownerId, Guid requirementId, CancellationToken cancellationToken);
}
