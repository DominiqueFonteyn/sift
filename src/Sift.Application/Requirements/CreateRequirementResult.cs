using Sift.Domain.Requirements;

namespace Sift.Application.Requirements;

public abstract record CreateRequirementResult
{
    private CreateRequirementResult()
    {
    }

    public sealed record Created(Requirement Requirement) : CreateRequirementResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : CreateRequirementResult;

    public sealed record Duplicate : CreateRequirementResult;
}
