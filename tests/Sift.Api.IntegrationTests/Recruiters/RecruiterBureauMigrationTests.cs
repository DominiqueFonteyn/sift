using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Sift.Infrastructure.Persistence;

namespace Sift.Api.IntegrationTests.Recruiters;

public sealed class RecruiterBureauMigrationTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private const string MigrationBeforeTheLink = "20261121155838_AddBureauWebsite";

    [Fact]
    public async Task Recruiters_created_before_the_bureau_column_still_list_without_a_bureau()
    {
        using var client = await AuthenticatedClient.CreateSignedInAsync(factory);
        var userId = await AuthenticatedClient.GetUserIdAsync(client);

        await MigrateAsync(MigrationBeforeTheLink);
        var legacyId = Guid.NewGuid();
        await InsertLegacyRecruiterAsync(legacyId, userId, "Legacy Larry");
        await MigrateAsync(null);

        var list = await client.GetFromJsonAsync<JsonElement>("/recruiters");
        var item = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal(legacyId, item.GetProperty("id").GetGuid());
        Assert.Equal("Legacy Larry", item.GetProperty("name").GetString());
        Assert.False(item.TryGetProperty("bureau", out _));
    }

    private async Task MigrateAsync(string? target)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SiftDbContext>();
        await context.GetService<IMigrator>().MigrateAsync(target);
    }

    private async Task InsertLegacyRecruiterAsync(Guid id, Guid ownerId, string name)
    {
        await using var connection = new SqliteConnection(factory.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "insert into Recruiters (Id, OwnerId, Name, CreatedAt, UpdatedAt) values ($id, $owner, $name, '2026-01-01 00:00:00+00:00', '2026-01-01 00:00:00+00:00')";
        command.Parameters.AddWithValue("$id", id.ToString().ToUpperInvariant());
        command.Parameters.AddWithValue("$owner", ownerId.ToString().ToUpperInvariant());
        command.Parameters.AddWithValue("$name", name);
        await command.ExecuteNonQueryAsync();
    }
}
