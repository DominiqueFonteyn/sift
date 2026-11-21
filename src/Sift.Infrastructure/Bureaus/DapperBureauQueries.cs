using System.Globalization;
using Dapper;
using Sift.Application.Bureaus;
using Sift.Application.Common;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Bureaus;

public sealed class DapperBureauQueries(ISiftConnectionFactory connectionFactory) : IBureauQueries
{
    private sealed record Row(string Id, string Name, string? Website, string CreatedAt);

    public async Task<PagedResult<BureauReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            """
            select count(*) from Bureaus where OwnerId = @OwnerId;
            select Id, Name, Website, CreatedAt from Bureaus where OwnerId = @OwnerId order by CreatedAt, Id limit @Limit offset @Offset;
            """,
            new { OwnerId = ownerId, Limit = page.PageSize, Offset = page.Offset },
            cancellationToken: cancellationToken);

        await using var results = await connection.QueryMultipleAsync(command);
        var total = await results.ReadSingleAsync<int>();
        var rows = await results.ReadAsync<Row>();

        return new PagedResult<BureauReadModel>(rows.Select(ToReadModel).ToList(), total);
    }

    public async Task<BureauReadModel?> GetAsync(Guid ownerId, Guid bureauId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            "select Id, Name, Website, CreatedAt from Bureaus where Id = @Id and OwnerId = @OwnerId;",
            new { Id = bureauId, OwnerId = ownerId },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<Row>(command);

        return row is null ? null : ToReadModel(row);
    }

    private static BureauReadModel ToReadModel(Row row) => new(Guid.Parse(row.Id), row.Name, row.Website, DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture));
}
