using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Sift.Api.IntegrationTests;

public sealed class SiftApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"sift-tests-{Guid.NewGuid():N}.db");

    public string DatabaseConnectionString => $"Data Source={databasePath};Pooling=False";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Program reads the connection string while building, before ConfigureAppConfiguration overrides apply.
        builder.UseSetting("ConnectionStrings:Sift", $"Data Source={databasePath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(databasePath))
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }
}
