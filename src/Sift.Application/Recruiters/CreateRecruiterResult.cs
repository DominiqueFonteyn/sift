using Sift.Application.Bureaus;
using Sift.Domain.Recruiters;

namespace Sift.Application.Recruiters;

public abstract record CreateRecruiterResult
{
    private CreateRecruiterResult()
    {
    }

    public sealed record Created(Recruiter Recruiter, BureauReadModel? Bureau) : CreateRecruiterResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : CreateRecruiterResult;

    // The bureau does not exist or is not owned by the caller; the two cases are not told apart.
    public sealed record BureauNotFound : CreateRecruiterResult;
}
