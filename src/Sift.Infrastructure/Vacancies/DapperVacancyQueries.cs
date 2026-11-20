using System.Globalization;
using Dapper;
using Sift.Application.Common;
using Sift.Application.Vacancies;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Vacancies;

public sealed class DapperVacancyQueries(ISiftConnectionFactory connectionFactory) : IVacancyQueries
{
    private sealed record Row(string Id, string Title, string Company, string CreatedAt);

    private sealed record DetailRow(string Id, string Title, string Company, string RawText, string CreatedAt);

    public async Task<PagedResult<VacancyListItem>> ListAsync(Guid ownerId, PageRequest page, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            """
            select count(*) from Vacancies where OwnerId = @OwnerId;
            select Id, Title, Company, CreatedAt from Vacancies where OwnerId = @OwnerId order by CreatedAt desc, Id desc limit @Limit offset @Offset;
            """,
            new { OwnerId = ownerId, Limit = page.PageSize, Offset = page.Offset },
            cancellationToken: cancellationToken);

        await using var results = await connection.QueryMultipleAsync(command);
        var total = await results.ReadSingleAsync<int>();
        var rows = await results.ReadAsync<Row>();

        return new PagedResult<VacancyListItem>(rows.Select(ToItem).ToList(), total);
    }

    public async Task<VacancyDetail?> GetAsync(Guid ownerId, Guid vacancyId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            "select Id, Title, Company, RawText, CreatedAt from Vacancies where Id = @Id and OwnerId = @OwnerId;",
            new { Id = vacancyId, OwnerId = ownerId },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<DetailRow>(command);

        return row is null
            ? null
            : new VacancyDetail(Guid.Parse(row.Id), row.Title, row.Company, row.RawText, DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture));
    }

    private static VacancyListItem ToItem(Row row) =>
        new(Guid.Parse(row.Id), row.Title, row.Company, DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture));
}
