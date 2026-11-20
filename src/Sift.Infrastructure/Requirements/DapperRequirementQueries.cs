using System.Globalization;
using Dapper;
using Sift.Application.Common;
using Sift.Application.Requirements;
using Sift.Domain.Requirements;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Requirements;

public sealed class DapperRequirementQueries(ISiftConnectionFactory connectionFactory) : IRequirementQueries
{
    private sealed record Row(string Id, string Text, string Kind, string CreatedAt);

    public async Task<PagedResult<RequirementReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            """
            select count(*) from Requirements where OwnerId = @OwnerId;
            select Id, Text, Kind, CreatedAt from Requirements where OwnerId = @OwnerId order by CreatedAt, Id limit @Limit offset @Offset;
            """,
            new { OwnerId = ownerId, Limit = page.PageSize, Offset = page.Offset },
            cancellationToken: cancellationToken);

        await using var results = await connection.QueryMultipleAsync(command);
        var total = await results.ReadSingleAsync<int>();
        var rows = await results.ReadAsync<Row>();

        return new PagedResult<RequirementReadModel>(rows.Select(ToReadModel).ToList(), total);
    }

    public async Task<IReadOnlyList<RequirementReadModel>> ListAllAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            "select Id, Text, Kind, CreatedAt from Requirements where OwnerId = @OwnerId order by CreatedAt, Id;",
            new { OwnerId = ownerId },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<Row>(command);

        return rows.Select(ToReadModel).ToList();
    }

    private static RequirementReadModel ToReadModel(Row row) => new(
        Guid.Parse(row.Id),
        row.Text,
        RequirementKinds.Parse(row.Kind),
        DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture));
}
