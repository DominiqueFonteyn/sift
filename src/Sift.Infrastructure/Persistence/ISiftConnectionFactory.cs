using System.Data.Common;

namespace Sift.Infrastructure.Persistence;

public interface ISiftConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken cancellationToken);
}
