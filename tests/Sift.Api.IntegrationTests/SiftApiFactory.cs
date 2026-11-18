using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sift.Application.Common;

namespace Sift.Api.IntegrationTests;

public sealed class SiftApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"sift-tests-{Guid.NewGuid():N}.db");

    public const string TestSigningKey = "integration-tests-only-signing-key-0123456789";

    public TestClock Clock { get; } = new();

    public string DatabaseConnectionString => $"Data Source={databasePath};Pooling=False";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Program reads the connection string while building, before ConfigureAppConfiguration overrides apply.
        builder.UseSetting("ConnectionStrings:Sift", $"Data Source={databasePath}");
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
        });
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
