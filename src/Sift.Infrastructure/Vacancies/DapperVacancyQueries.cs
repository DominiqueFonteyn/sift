using System.Globalization;
using Dapper;
using Sift.Application.Vacancies;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Vacancies;

public sealed class DapperVacancyQueries(ISiftConnectionFactory connectionFactory) : IVacancyQueries
{
    private sealed record DetailRow(string Id, string Title, string Company, string RawText, string CreatedAt);

    public async Task<IReadOnlyList<VacancyDetail>> ListAllAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            "select Id, Title, Company, RawText, CreatedAt from Vacancies where OwnerId = @OwnerId;",
            new { OwnerId = ownerId },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<DetailRow>(command);

        return rows.Select(ToDetail).ToList();
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
            : ToDetail(row);
    }

    private static VacancyDetail ToDetail(DetailRow row) =>
        new(Guid.Parse(row.Id), row.Title, row.Company, row.RawText, DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture));
}
