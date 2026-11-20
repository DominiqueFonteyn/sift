using System.Globalization;
using Dapper;
using Sift.Application.Common;
using Sift.Application.Recruiters;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Recruiters;

public sealed class DapperRecruiterQueries(ISiftConnectionFactory connectionFactory) : IRecruiterQueries
{
    private sealed record Row(string Id, string Name, string? Email, string? Phone, string? LinkedInUrl, string CreatedAt);

    public async Task<PagedResult<RecruiterReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            """
            select count(*) from Recruiters where OwnerId = @OwnerId;
            select Id, Name, Email, Phone, LinkedInUrl, CreatedAt from Recruiters where OwnerId = @OwnerId order by CreatedAt, Id limit @Limit offset @Offset;
            """,
            new { OwnerId = ownerId, Limit = page.PageSize, Offset = page.Offset },
            cancellationToken: cancellationToken);

        await using var results = await connection.QueryMultipleAsync(command);
        var total = await results.ReadSingleAsync<int>();
        var rows = await results.ReadAsync<Row>();

        return new PagedResult<RecruiterReadModel>(rows.Select(ToReadModel).ToList(), total);
    }

    private static RecruiterReadModel ToReadModel(Row row) => new(
        Guid.Parse(row.Id),
        row.Name,
        row.Email,
        row.Phone,
        row.LinkedInUrl,
        DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture));
}
