using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Sift.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory(string connectionString) : ISiftConnectionFactory
{
    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return connection;
    }
}
