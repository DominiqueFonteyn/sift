using Sift.Domain.Resumes;

namespace Sift.Application.Resumes;

public abstract record SaveMyResumeResult
{
    private SaveMyResumeResult()
    {
    }

    public sealed record Saved(Resume Resume) : SaveMyResumeResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : SaveMyResumeResult;
}
