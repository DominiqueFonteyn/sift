using Microsoft.EntityFrameworkCore;
using Sift.Application.Resumes;
using Sift.Domain.Resumes;
using Sift.Infrastructure.Persistence;

namespace Sift.Infrastructure.Resumes;

public sealed class EfResumeRepository(SiftDbContext dbContext) : IResumeRepository
{
    public Task<Resume?> FindByOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        dbContext.Set<Resume>().SingleOrDefaultAsync(resume => resume.OwnerId == ownerId, cancellationToken);

    public async Task AddAsync(Resume resume, CancellationToken cancellationToken)
    {
        dbContext.Set<Resume>().Add(resume);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(Resume resume, CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
