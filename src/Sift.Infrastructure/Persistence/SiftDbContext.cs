using Microsoft.EntityFrameworkCore;

namespace Sift.Infrastructure.Persistence;

public sealed class SiftDbContext(DbContextOptions<SiftDbContext> options) : DbContext(options)
{
}
