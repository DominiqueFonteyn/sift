using Sift.Domain.Recruiters;

namespace Sift.Application.Recruiters;

public interface IRecruiterRepository
{
    Task AddAsync(Recruiter recruiter, CancellationToken cancellationToken);
}
