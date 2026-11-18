using Dapper;
using Sift.Application.Health;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Health;

public sealed class SqliteDatabaseProbe(ISiftConnectionFactory connectionFactory) : IDatabaseProbe
{
    public async Task<bool> CanReadAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition("select 1;", cancellationToken: cancellationToken);
        var result = await connection.ExecuteScalarAsync<long>(command);

        return result == 1;
    }
}
