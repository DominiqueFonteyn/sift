using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sift.Application.Health;
using Sift.Infrastructure.Health;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSiftInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SiftDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton<ISiftConnectionFactory>(_ => new SqliteConnectionFactory(connectionString));
        services.AddScoped<IDatabaseProbe, SqliteDatabaseProbe>();

        return services;
    }
}
