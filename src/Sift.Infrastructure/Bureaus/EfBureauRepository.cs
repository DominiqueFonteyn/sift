using Sift.Application.Bureaus;
using Sift.Domain.Bureaus;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Bureaus;

public sealed class EfBureauRepository(SiftDbContext dbContext) : IBureauRepository
{
    public async Task AddAsync(Bureau bureau, CancellationToken cancellationToken)
    {
        dbContext.Set<Bureau>().Add(bureau);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
