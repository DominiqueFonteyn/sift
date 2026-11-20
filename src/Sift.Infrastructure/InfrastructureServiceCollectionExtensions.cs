using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sift.Application.Bureaus;
using Sift.Application.Common;
using Sift.Application.Health;
using Sift.Application.Identity;
using Sift.Application.Resumes;
using Sift.Application.Vacancies;
using Sift.Infrastructure.Bureaus;
using Sift.Infrastructure.Common;
using Sift.Infrastructure.Health;
using Sift.Infrastructure.Identity;
using Sift.Infrastructure.Persistence;
using Sift.Infrastructure.Resumes;
using Sift.Infrastructure.Vacancies;

namespace Sift.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSiftInfrastructure(this IServiceCollection services, string connectionString, JwtOptions jwtOptions)
    {
        jwtOptions.EnsureValid();
        services.AddSingleton(jwtOptions);
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddDbContext<SiftDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton<ISiftConnectionFactory>(_ => new SqliteConnectionFactory(connectionString));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IDatabaseProbe, SqliteDatabaseProbe>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<ICurrentUserQuery, DapperCurrentUserQuery>();
        services.AddScoped<IBureauRepository, EfBureauRepository>();
        services.AddScoped<IBureauQueries, DapperBureauQueries>();
        services.AddScoped<IResumeRepository, EfResumeRepository>();
        services.AddScoped<IResumeQueries, DapperResumeQueries>();
        services.AddScoped<IVacancyRepository, EfVacancyRepository>();
        services.AddScoped<IVacancyQueries, DapperVacancyQueries>();

        return services;
    }
}
