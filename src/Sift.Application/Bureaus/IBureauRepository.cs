using Sift.Domain.Bureaus;

namespace Sift.Application.Bureaus;

public interface IBureauRepository
{
    Task AddAsync(Bureau bureau, CancellationToken cancellationToken);
}
