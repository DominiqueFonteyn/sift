using Sift.Application.Recruiters;
using Sift.Domain.Recruiters;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Recruiters;

public sealed class EfRecruiterRepository(SiftDbContext dbContext) : IRecruiterRepository
{
    public async Task AddAsync(Recruiter recruiter, CancellationToken cancellationToken)
    {
        dbContext.Set<Recruiter>().Add(recruiter);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
