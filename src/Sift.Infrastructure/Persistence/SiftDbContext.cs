using Microsoft.EntityFrameworkCore;

namespace Sift.Infrastructure.Persistence;

public sealed class SiftDbContext(DbContextOptions<SiftDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SiftDbContext).Assembly);
}
