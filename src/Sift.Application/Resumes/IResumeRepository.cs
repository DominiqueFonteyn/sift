using Sift.Domain.Resumes;

namespace Sift.Application.Resumes;

public interface IResumeRepository
{
    Task<Resume?> FindByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task AddAsync(Resume resume, CancellationToken cancellationToken);

    Task SaveAsync(Resume resume, CancellationToken cancellationToken);
}
