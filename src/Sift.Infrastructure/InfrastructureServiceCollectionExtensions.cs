using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sift.Application.Common;
using Sift.Application.Health;
using Sift.Application.Identity;
using Sift.Infrastructure.Common;
using Sift.Infrastructure.Health;
using Sift.Infrastructure.Identity;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSiftInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SiftDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton<ISiftConnectionFactory>(_ => new SqliteConnectionFactory(connectionString));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IDatabaseProbe, SqliteDatabaseProbe>();
        services.AddScoped<IUserRepository, EfUserRepository>();

        return services;
    }
}
