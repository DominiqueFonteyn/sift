namespace Sift.Application.Resumes;

public interface IResumeQueries
{
    // Null when the user has no resume yet.
    Task<ResumeReadModel?> GetAsync(Guid ownerId, CancellationToken cancellationToken);
}
