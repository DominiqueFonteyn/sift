using System.Globalization;
using Dapper;
using Sift.Application.Bureaus;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Bureaus;

public sealed class DapperBureauQueries(ISiftConnectionFactory connectionFactory) : IBureauQueries
{
    private sealed record Row(string Id, string Name, string CreatedAt);

    public async Task<IReadOnlyList<BureauReadModel>> ListAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            "select Id, Name, CreatedAt from Bureaus where OwnerId = @OwnerId order by CreatedAt, Id;",
            new { OwnerId = ownerId },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<Row>(command);

        return rows.Select(ToReadModel).ToList();
    }

    public async Task<BureauReadModel?> GetAsync(Guid ownerId, Guid bureauId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            "select Id, Name, CreatedAt from Bureaus where Id = @Id and OwnerId = @OwnerId;",
            new { Id = bureauId, OwnerId = ownerId },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<Row>(command);

        return row is null ? null : ToReadModel(row);
    }

    private static BureauReadModel ToReadModel(Row row) => new(Guid.Parse(row.Id), row.Name, DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture));
}
