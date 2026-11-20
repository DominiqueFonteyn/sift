namespace Sift.Application.Requirements;

public abstract record DeleteRequirementResult
{
    private DeleteRequirementResult()
    {
    }

    public sealed record Deleted : DeleteRequirementResult;

    public sealed record NotFound : DeleteRequirementResult;
}
