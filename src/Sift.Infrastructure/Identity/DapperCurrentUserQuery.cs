using Dapper;
using Sift.Application.Identity;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Identity;

public sealed class DapperCurrentUserQuery(ISiftConnectionFactory connectionFactory) : ICurrentUserQuery
{
    public async Task<CurrentUserReadModel?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            "select Id, Email from Users where Id = @Id;",
            new { Id = userId },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<(string Id, string Email)?>(command);

        return row is { } found ? new CurrentUserReadModel(Guid.Parse(found.Id), found.Email) : null;
    }
}
