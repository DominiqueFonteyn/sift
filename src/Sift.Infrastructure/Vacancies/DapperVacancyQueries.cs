using System.Globalization;
using Dapper;
using Sift.Application.Common;
using Sift.Application.Vacancies;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Vacancies;

public sealed class DapperVacancyQueries(ISiftConnectionFactory connectionFactory) : IVacancyQueries
{
    private sealed record Row(string Id, string Title, string Company, string CreatedAt);

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

    private static VacancyListItem ToItem(Row row) =>
        new(Guid.Parse(row.Id), row.Title, row.Company, DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture));
}
