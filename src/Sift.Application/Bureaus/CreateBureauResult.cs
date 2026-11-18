using Sift.Domain.Bureaus;

namespace Sift.Application.Bureaus;

public abstract record CreateBureauResult
{
    private CreateBureauResult()
    {
    }

    public sealed record Created(Bureau Bureau) : CreateBureauResult;

    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : CreateBureauResult;
}
