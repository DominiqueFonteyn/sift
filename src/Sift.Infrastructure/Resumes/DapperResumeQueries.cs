using System.Globalization;
using Dapper;
using Sift.Application.Resumes;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Resumes;

public sealed class DapperResumeQueries(ISiftConnectionFactory connectionFactory) : IResumeQueries
{
    private sealed record Row(string Text, string UpdatedAt);

    public async Task<ResumeReadModel?> GetAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            "select Text, UpdatedAt from Resumes where OwnerId = @OwnerId;",
            new { OwnerId = ownerId },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<Row>(command);

        return row is null ? null : new ResumeReadModel(row.Text, DateTimeOffset.Parse(row.UpdatedAt, CultureInfo.InvariantCulture));
    }
}
