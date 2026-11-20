using Sift.Domain.Recruiters;

namespace Sift.Application.Recruiters;

public abstract record CreateRecruiterResult
{
    private CreateRecruiterResult()
    {
    }

    public sealed record Created(Recruiter Recruiter) : CreateRecruiterResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : CreateRecruiterResult;
}
