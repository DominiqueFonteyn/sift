using System.Globalization;
using Dapper;
using Sift.Application.Common;
using Sift.Application.Recruiters;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Recruiters;

public sealed class DapperRecruiterQueries(ISiftConnectionFactory connectionFactory) : IRecruiterQueries
{
    private sealed record Row(string Id, string Name, string? Email, string? Phone, string? LinkedInUrl, string CreatedAt, string? BureauId, string? BureauName);

    public async Task<PagedResult<RecruiterReadModel>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        // The join is scoped to the same owner, so a stray foreign bureau id could never leak a name.
        var command = new CommandDefinition(
            """
            select count(*) from Recruiters where OwnerId = @OwnerId;
            select r.Id, r.Name, r.Email, r.Phone, r.LinkedInUrl, r.CreatedAt, b.Id as BureauId, b.Name as BureauName
            from Recruiters r
            left join Bureaus b on b.Id = r.BureauId and b.OwnerId = r.OwnerId
            where r.OwnerId = @OwnerId
            order by r.CreatedAt, r.Id
            limit @Limit offset @Offset;
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
        DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture),
        row.BureauId is null || row.BureauName is null ? null : new RecruiterBureauReadModel(Guid.Parse(row.BureauId), row.BureauName));
}
